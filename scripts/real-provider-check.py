#!/usr/bin/env python3
"""Run opt-in, privacy-preserving checks against locally connected real accounts."""

import argparse
import json
import queue
import subprocess
import threading
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path


class CheckFailure(RuntimeError):
    """A sanitized test failure that is safe to print."""


def require(condition, stage, message=None):
    if not condition:
        raise CheckFailure(message or stage)


def search_terms(provider):
    if provider == "google":
        return ["newer_than:3650d", "in:anywhere"]
    if provider == "microsoft":
        return ["Microsoft", "account", "security", "welcome"]
    return ["mail"]


def main():
    parser = argparse.ArgumentParser(
        description="Check real MailMeUp accounts locally. This script is not for CI."
    )
    parser.add_argument("executable", type=Path, help="Path to mailmeup or mailmeup.exe")
    args = parser.parse_args()
    executable = args.executable.resolve(strict=True)

    process = subprocess.Popen(
        [str(executable), "--stdio"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.DEVNULL,
        text=True,
        encoding="utf-8",
    )
    messages = queue.Queue()

    def consume_stdout():
        for line in process.stdout:
            try:
                messages.put(json.loads(line))
            except json.JSONDecodeError:
                messages.put(CheckFailure("MCP stdout contained non-protocol output."))
        messages.put(CheckFailure("MCP stdout closed unexpectedly."))

    threading.Thread(target=consume_stdout, daemon=True).start()

    def send(message):
        process.stdin.write(json.dumps({"jsonrpc": "2.0", **message}) + "\n")
        process.stdin.flush()

    def receive(identifier):
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline:
            try:
                item = messages.get(timeout=max(0.1, deadline - time.monotonic()))
            except queue.Empty as error:
                raise CheckFailure("MCP response timed out.") from error
            if isinstance(item, Exception):
                raise item
            if item.get("id") == identifier:
                return item
        raise CheckFailure("MCP response timed out.")

    next_identifier = 1

    def request(method, params=None):
        nonlocal next_identifier
        identifier = next_identifier
        next_identifier += 1
        message = {"id": identifier, "method": method}
        if params is not None:
            message["params"] = params
        send(message)
        return receive(identifier)

    def content(name, arguments):
        response = request("tools/call", {"name": name, "arguments": arguments})
        require("error" not in response, f"{name}_protocol")
        result = response["result"]
        require(not result.get("isError", False), f"{name}_tool")
        return result.get("structuredContent") or json.loads(result["content"][0]["text"])

    summary = {"success": False}
    try:
        initialized = request(
            "initialize",
            {
                "protocolVersion": "2025-11-25",
                "capabilities": {},
                "clientInfo": {"name": "mailmeup-real-provider-check", "version": "1.0.0"},
            },
        )
        require("result" in initialized, "mcp_initialize")
        send({"method": "notifications/initialized"})

        tools = request("tools/list")["result"]["tools"]
        require(len(tools) == 7, "read_only_tool_count")
        require(
            all(tool.get("annotations", {}).get("readOnlyHint") is True for tool in tools),
            "read_only_annotations",
        )

        accounts = content("list_accounts", {})["accounts"]
        require(
            len(accounts) >= 2,
            "minimum_account_count",
            "Real multi-account checks require at least two connected accounts.",
        )
        ordinals = {account["id"]: index for index, account in enumerate(accounts, start=1)}

        account_checks = [
            {
                "ordinal": ordinals[account["id"]],
                "provider": account["provider"],
                "mail_checked": False,
                "mail_sample_found": False,
                "mail_detail_consistent": False,
                "mail_cursor_checked": False,
                "calendar_checked": False,
                "calendar_count": 0,
                "event_sample_found": False,
                "event_detail_consistent": False,
            }
            for account in accounts
        ]
        checks_by_ordinal = {check["ordinal"]: check for check in account_checks}
        mail_sample_count = 0
        mail_detail_consistency_count = 0
        cursor_check_count = 0
        mail_accounts = [account for account in accounts if account["mail_read_enabled"]]

        for account in mail_accounts:
            check = checks_by_ordinal[ordinals[account["id"]]]
            check["mail_checked"] = True
            result = None
            selected_query = None
            for candidate in search_terms(account["provider"]):
                current = content(
                    "search_mail",
                    {"query": candidate, "accountIds": [account["id"]], "limit": 3},
                )
                require(current["coverage_complete"] is True, "single_account_mail_coverage")
                require(len(current["searched_account_ids"]) == 1, "single_account_mail_count")
                require(len(current["failed_accounts"]) == 0, "single_account_mail_failure")
                result = current
                selected_query = candidate
                if current["items"]:
                    break

            sample_found = bool(result and result["items"])
            detail_consistent = False
            if sample_found:
                item = result["items"][0]
                detail = content(
                    "read_mail",
                    {"reference": item["reference"], "maxCharacters": 1000},
                )
                detail_consistent = (
                    detail["account_id"] == account["id"]
                    and detail["subject"] == item["subject"]
                    and detail["sender"] == item["sender"]
                    and detail["received_at"] == item["received_at"]
                    and isinstance(detail["text"], str)
                    and len(detail["text"]) <= 1000
                )
                require(detail_consistent, "mail_summary_detail_mismatch")
                mail_sample_count += 1
                mail_detail_consistency_count += 1

            cursor_checked = False
            if result and result.get("next_cursor"):
                next_page = content(
                    "search_mail",
                    {
                        "query": selected_query,
                        "accountIds": [account["id"]],
                        "limit": 3,
                        "cursor": result["next_cursor"],
                    },
                )
                require(next_page["coverage_complete"] is True, "single_account_cursor_coverage")
                cursor_checked = True
                cursor_check_count += 1

            check["mail_sample_found"] = sample_found
            check["mail_detail_consistent"] = detail_consistent
            check["mail_cursor_checked"] = cursor_checked

        calendar_accounts = [account for account in accounts if account["calendar_read_enabled"]]
        calendar_ids = [account["id"] for account in calendar_accounts]
        calendars = content("list_calendars", {"accountIds": calendar_ids})
        require(calendars["coverage_complete"] is True, "calendar_coverage")
        require(len(calendars["searched_account_ids"]) == len(calendar_ids), "calendar_account_count")
        require(len(calendars["failed_accounts"]) == 0, "calendar_failures")

        end = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
        start = end - timedelta(days=30)
        event_sample_count = 0
        event_detail_consistency_count = 0

        for account in calendar_accounts:
            ordinal = ordinals[account["id"]]
            check = checks_by_ordinal[ordinal]

            account_calendars = [
                calendar
                for calendar in calendars["calendars"]
                if calendar["account_id"] == account["id"]
            ]
            check["calendar_checked"] = True
            check["calendar_count"] = len(account_calendars)
            if not account_calendars:
                continue

            try:
                events = content(
                    "search_events",
                    {
                        "start": start.isoformat(),
                        "end": end.isoformat(),
                        "calendarReferences": [calendar["reference"] for calendar in account_calendars],
                        "limit": 3,
                    },
                )
            except CheckFailure as error:
                raise CheckFailure(
                    f"account_{ordinal} ({account['provider']}) event search failed"
                ) from error
            require(events["coverage_complete"] is True, "single_account_event_coverage")
            require(len(events["failed_accounts"]) == 0, "single_account_event_failure")
            if not events["events"]:
                continue

            event = events["events"][0]
            detail = content(
                "read_event",
                {"reference": event["reference"], "maxDescriptionCharacters": 1000},
            )
            detail_consistent = (
                detail["account_id"] == account["id"]
                and detail["title"] == event["title"]
                and detail["start"] == event["start"]
                and detail["end"] == event["end"]
                and isinstance(detail["description"], str)
                and len(detail["description"]) <= 1000
            )
            require(detail_consistent, "event_summary_detail_mismatch")
            check["event_sample_found"] = True
            check["event_detail_consistent"] = True
            event_sample_count += 1
            event_detail_consistency_count += 1

        all_mail_ids = [account["id"] for account in mail_accounts]
        mixed_mail = content(
            "search_mail",
            {"query": "Microsoft", "accountIds": all_mail_ids, "limit": 8},
        )
        require(mixed_mail["coverage_complete"] is True, "mixed_mail_coverage")
        require(len(mixed_mail["searched_account_ids"]) == len(all_mail_ids), "mixed_mail_account_count")
        require(len(mixed_mail["failed_accounts"]) == 0, "mixed_mail_failures")

        account_checks.sort(key=lambda check: check["ordinal"])
        summary = {
            "success": True,
            "manual_only": True,
            "read_only_tool_count": len(tools),
            "account_count": len(accounts),
            "checked_accounts": [f"account_{index}" for index in range(1, len(accounts) + 1)],
            "account_checks": account_checks,
            "mail_account_count": len(mail_accounts),
            "mail_sample_count": mail_sample_count,
            "mail_detail_consistency_count": mail_detail_consistency_count,
            "mail_cursor_check_count": cursor_check_count,
            "mixed_mail_coverage_complete": mixed_mail["coverage_complete"],
            "calendar_account_count": len(calendar_accounts),
            "calendar_count": len(calendars["calendars"]),
            "event_sample_count": event_sample_count,
            "event_detail_consistency_count": event_detail_consistency_count,
        }
    except CheckFailure as error:
        summary = {"success": False, "manual_only": True, "error": str(error)}
    finally:
        if process.stdin:
            process.stdin.close()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)

    print(json.dumps(summary, separators=(",", ":")))
    return 0 if summary["success"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
