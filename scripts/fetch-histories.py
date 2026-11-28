"""Download the server's finished games, for the bots to be checked against (BlunderRun):

    py -3 scripts/fetch-histories.py [--server https://zaturnava.runasp.net/] [--out artifacts/blunders/histories.json]
    py -3 scripts/fetch-histories.py --game GAME_ID       (one game, finished or not)
    py -3 scripts/fetch-histories.py --report REPORT_ID   (a bug report, and its game)

Writes every finished game's history (api/history) as one JSON array. With the file there, the BotRunner tests'
BlunderRun replays each game and looks for bots' moves that lost a piece at once; without it, BlunderRun is skipped.
With --game, that one game's history, as a one-game array, to artifacts/reports/GAME_ID.json; with --report, the
report (api/bug-reports/REPORT_ID) to artifacts/reports/REPORT_ID.report.json, and its game's history beside it,
cut to the point the game had reached when the report was sent.
"""

import argparse
import json
import os
import urllib.request


def fetched(server, path):
    """The JSON the server answers at the path."""
    request = urllib.request.Request(server.rstrip("/") + "/" + path, headers={"User-Agent": "histories"})
    with urllib.request.urlopen(request, timeout=120) as response:
        return json.load(response)


def written(path, value):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as out:
        json.dump(value, out, indent=1)
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="https://zaturnava.runasp.net/")
    parser.add_argument("--out", default=os.path.join("artifacts", "blunders", "histories.json"))
    parser.add_argument("--game")
    parser.add_argument("--report")
    arguments = parser.parse_args()
    folder = os.path.join("artifacts", "reports")
    if arguments.report:
        report = fetched(arguments.server, f"api/bug-reports/{arguments.report}")
        print(f"report written to {written(os.path.join(folder, arguments.report + '.report.json'), report)}")
        history = fetched(arguments.server, f"api/games/{report['gameId']}/history")
        history["actions"] = history["actions"][: report["actions"]]
        path = written(os.path.join(folder, report["gameId"] + ".json"), [history])
        print(f"{report['description']!r}: game {report['gameId']}, {report['actions']} actions, to {path}")
        return
    if arguments.game:
        history = fetched(arguments.server, f"api/games/{arguments.game}/history")
        path = written(os.path.join(folder, arguments.game + ".json"), [history])
        print(f"{len(history['actions'])} actions written to {path}")
        return
    games = fetched(arguments.server, "api/history")
    written(arguments.out, games)
    print(f"{len(games)} finished games written to {arguments.out}")


if __name__ == "__main__":
    main()
