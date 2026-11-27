"""Download the server's finished games, for the bots to be checked against (BlunderRun):

    py -3 scripts/fetch-histories.py [--server https://zaturnava.runasp.net/] [--out artifacts/blunders/histories.json]

Writes every finished game's history (api/history) as one JSON array. With the file there, the BotRunner tests'
BlunderRun replays each game and looks for bots' moves that lost a piece at once; without it, BlunderRun is skipped.
"""

import argparse
import json
import os
import urllib.request


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="https://zaturnava.runasp.net/")
    parser.add_argument("--out", default=os.path.join("artifacts", "blunders", "histories.json"))
    arguments = parser.parse_args()
    request = urllib.request.Request(arguments.server.rstrip("/") + "/api/history", headers={"User-Agent": "histories"})
    with urllib.request.urlopen(request, timeout=120) as response:
        games = json.load(response)
    os.makedirs(os.path.dirname(arguments.out), exist_ok=True)
    with open(arguments.out, "w", encoding="utf-8") as out:
        json.dump(games, out)
    print(f"{len(games)} finished games written to {arguments.out}")


if __name__ == "__main__":
    main()
