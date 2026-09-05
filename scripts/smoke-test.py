#!/usr/bin/env python3
"""Exercise a built DLL or native executable through the real CLI and MCP stdio boundary."""

import argparse
import json
import os
from pathlib import Path
import queue
import subprocess
import tempfile
import threading
import time


def check(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    args = parser.parse_args()
    executable = args.executable.resolve(strict=True)
    command = ["dotnet", str(executable)] if executable.suffix == ".dll" else [str(executable)]

    with tempfile.TemporaryDirectory(prefix="mailmeup-smoke-") as directory:
        registry = str(Path(directory) / "registry")
        environment = {**os.environ, "MAILMEUP_DATA_DIR": registry, "DOTNET_CLI_UI_LANGUAGE": "en"}
        for cli_args in (["--help"], ["--version"], ["status"], ["accounts", "list"], ["setup", "status"]):
            result = subprocess.run(command + cli_args, env=environment, capture_output=True, text=True, timeout=30, check=True)
            check(bool(result.stdout.strip()), f"Empty output for {cli_args}")
            if cli_args == ["status"]:
                status = json.loads(result.stdout)
                check(status["stage"] == "read_only_mvp", "Incorrect application stage")
                check(status["can_connect_accounts"] is True, "Account connection was not advertised")
                check(status["read_only"] is True, "Read-only scope was not reported")
                check(all(provider["authentication_available"] for provider in status["providers"]), "Missing authentication capability")
                check(all(provider["mail_read_available"] for provider in status["providers"]), "Missing mail capability")
                check(all(provider["calendar_read_available"] for provider in status["providers"]), "Missing calendar capability")
            if cli_args == ["accounts", "list"]:
                check(json.loads(result.stdout) == {"accounts": []}, "Unexpected account data")
            if cli_args == ["setup", "status"]:
                providers = json.loads(result.stdout)["providers"]
                check({provider["provider_id"] for provider in providers} == {"google", "microsoft"}, "Unexpected setup providers")
                check(all(not provider["configured"] for provider in providers), "Fresh setup should be empty")
        invalid = subprocess.run(command + ["unknown"], env=environment, capture_output=True, text=True, timeout=30)
        check(invalid.returncode == 2 and not invalid.stdout, "Unknown commands must fail on stderr")

        process = subprocess.Popen(command + ["--stdio"], env=environment, stdin=subprocess.PIPE,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8")
        messages = queue.Queue()
        errors = []

        def consume_stdout():
            for line in process.stdout:
                try:
                    messages.put(json.loads(line))
                except json.JSONDecodeError:
                    messages.put(RuntimeError("Non-JSON output on MCP stdout"))
            messages.put(RuntimeError("MCP stdout closed unexpectedly"))

        def consume_stderr():
            for line in process.stderr:
                errors.append(line)

        threading.Thread(target=consume_stdout, daemon=True).start()
        threading.Thread(target=consume_stderr, daemon=True).start()

        def send(message):
            process.stdin.write(json.dumps({"jsonrpc": "2.0", **message}) + "\n")
            process.stdin.flush()

        def receive(identifier):
            deadline = time.monotonic() + 30
            while True:
                item = messages.get(timeout=max(0.1, deadline - time.monotonic()))
                if isinstance(item, Exception):
                    raise item
                if item.get("id") == identifier:
                    return item
                check(time.monotonic() < deadline, "MCP response timeout")

        def content(response):
            check("error" not in response, "MCP returned a protocol error")
            result = response["result"]
            check(not result.get("isError", False), "MCP tool returned an error")
            return result.get("structuredContent") or json.loads(result["content"][0]["text"])

        try:
            send({"id": 1, "method": "initialize", "params": {"protocolVersion": "2025-11-25", "capabilities": {},
                  "clientInfo": {"name": "mailmeup-smoke", "version": "1.0.0"}}})
            check("result" in receive(1), "MCP initialization failed")
            send({"method": "notifications/initialized"})
            send({"id": 2, "method": "tools/list"})
            tools = receive(2)["result"]["tools"]
            expected_tools = {"get_status", "list_accounts", "search_mail", "read_mail",
                              "list_calendars", "search_events", "read_event"}
            check({tool["name"] for tool in tools} == expected_tools, "Unexpected tool surface")
            check(all(tool["annotations"]["readOnlyHint"] for tool in tools), "Missing read-only hints")
            send({"id": 3, "method": "tools/call", "params": {"name": "get_status", "arguments": {}}})
            check(content(receive(3))["can_connect_accounts"] is True, "Incorrect MCP readiness")
            send({"id": 4, "method": "tools/call", "params": {"name": "list_accounts", "arguments": {}}})
            check(content(receive(4)) == {"accounts": []}, "Unexpected MCP account data")
            send({"id": 5, "method": "tools/call", "params": {"name": "search_mail", "arguments": {"query": "sample"}}})
            mail = content(receive(5))
            check(mail["items"] == [] and mail["coverage_complete"] is True, "Unexpected empty mail search")
            send({"id": 6, "method": "tools/call", "params": {"name": "list_calendars", "arguments": {}}})
            calendars = content(receive(6))
            check(calendars["calendars"] == [] and calendars["coverage_complete"] is True, "Unexpected empty calendar list")
            send({"id": 7, "method": "tools/call", "params": {"name": "search_events", "arguments": {
                "start": "2026-09-05T00:00:00+07:00", "end": "2026-09-06T00:00:00+07:00"}}})
            events = content(receive(7))
            check(events["events"] == [] and events["coverage_complete"] is True, "Unexpected empty event search")
            for identifier, name, arguments in ((8, "read_mail", {"reference": "m_invalid"}),
                                                (9, "read_event", {"reference": "evt_invalid"})):
                send({"id": identifier, "method": "tools/call", "params": {"name": name, "arguments": arguments}})
                rejected = receive(identifier)
                check("error" in rejected or rejected.get("result", {}).get("isError", False),
                      f"Invalid reference was accepted by {name}")
        finally:
            process.stdin.close()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)
        check(not Path(registry).exists(), "Discovery created local state")
    print("PASS: CLI commands, errors, seven-tool MCP surface, empty reads, invalid references, and stateless first run.")


if __name__ == "__main__":
    main()
