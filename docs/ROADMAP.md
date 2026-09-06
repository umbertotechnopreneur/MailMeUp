# Roadmap

**All planned provider features are read-only.** Write actions require a separate future decision.

For the practical steps and completion checks, see the [MVP delivery plan](MVP_PLAN.md).

| Step | Result |
| --- | --- |
| 0. Foundation | Repository, brand, executable, discovery tools, tests and packaging |
| 1. Account setup | Browser sign-in, multiple accounts and protected credentials |
| 2. Email reads | Provider search and selected-message reading |
| 3. Unified mail tools | Search across accounts with short results and clear coverage |
| 3C. Calendars | Calendar discovery, appointment search and a unified agenda |
| 4. Public preview | Simple installation, provider registration readiness and platform checks |

Each step needs working tests before it is advertised as available.

The current source passed 111 synthetic tests. Step 4 includes an installed Windows MSIX / WinUI 3 preview, a generated About banner and GitHub support links; an upgrade, installed existing-registry startup and the alias MCP smoke passed. The earlier CLI build passed real reads across four accounts. Next: clean-machine installation, UI sign-in/Codex plugin setup, deliberate real recovery scenarios and a small pilot.

A bundled local Codex plugin is included in the Windows setup scope. No public marketplace publication, web dashboard or hosted service is planned.
