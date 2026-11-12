"""Pin the .NET SDK a CI run builds with, in global.json, to the one the code is checked with on the developer's own
machine, so that a newer SDK on the runner, bringing new analyzer rules (LangVersion and AnalysisLevel are `latest`),
cannot fail a check the code passed there (2026-11-12: a runner's newer SDK flagged IDE0028 and CA2026 on code
that had passed). Run by CI only, after setup-dotnet has installed that SDK; the global.json in the repository
stays unpinned, so any 10.0 SDK builds it locally.

    python3 scripts/pin-sdk.py

When the developer's SDK moves on, change SDK below, and setup-dotnet's version in every workflow under
.github/workflows with it.
"""

import json
from pathlib import Path

SDK = "10.0.403"

GLOBAL_JSON = Path(__file__).resolve().parent.parent / "global.json"


def main() -> None:
    settings = json.loads(GLOBAL_JSON.read_text(encoding="utf-8"))
    settings["sdk"] = {"version": SDK, "rollForward": "disable"}
    GLOBAL_JSON.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")
    print(f"global.json pinned to SDK {SDK}")


if __name__ == "__main__":
    main()
