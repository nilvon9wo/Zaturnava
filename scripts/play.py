#!/usr/bin/env python3
"""
Build, then run the game server (http://localhost:5164) and the web page
(http://localhost:5165) side by side until Ctrl+C, which stops both.

Each runs with its launch profile, so the same ports and environment as
Visual Studio's F5. The two are built one after the other first: two builds at
once collide on the shared, woven assemblies.

Run it again to restart. Don't rebuild these projects while they are running:
the page server indexes its files once, at startup, and a rebuild beneath it
leaves the page stuck on "Loading...".
"""
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECTS = ["src/Hosts/Zaturnava.Server", "src/Hosts/Zaturnava.Web"]
PAGE = "http://localhost:5165"


def main() -> int:
    for project in PROJECTS:
        if subprocess.run(["dotnet", "build", project, "-nologo", "-v", "quiet"], cwd=ROOT).returncode != 0:
            return 1
    running = [
        subprocess.Popen(["dotnet", "run", "--project", project, "--no-build"], cwd=ROOT)
        for project in PROJECTS
    ]
    print(f"Playing at {PAGE} - Ctrl+C stops both.", flush=True)
    try:
        for process in running:
            process.wait()
    except KeyboardInterrupt:
        pass
    finally:
        for process in running:
            process.terminate()
        for process in running:
            process.wait()
    return 0


if __name__ == "__main__":
    sys.exit(main())
