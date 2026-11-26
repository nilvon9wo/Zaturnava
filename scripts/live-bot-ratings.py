"""Prints every bot's rating at each ruleset, as the live server's catalog gives them, lowest first.

Usage: py -3 scripts/live-bot-ratings.py [base-url] [ruleset]

Only the catalog, which anyone may ask for, no token sent.
"""

import json
import sys
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "https://zaturnava.runasp.net/"
RULESET = sys.argv[2] if len(sys.argv) > 2 else "chaturaji-2027"


def main() -> None:
    request = urllib.request.Request(BASE + "api/catalog", headers={"User-Agent": "zaturnava-ratings"})
    with urllib.request.urlopen(request, timeout=120) as response:
        catalog = json.load(response)
    bots = [(bot.get("ratings", {}).get(RULESET), bot["name"], bot["id"]) for bot in catalog["bots"]]
    for rating, name, bot_id in sorted(bots, key=lambda bot: (bot[0] is None, bot[0] or 0)):
        shown = "-" if rating is None else str(rating)
        print(f"{shown:>6}  {name:20} {bot_id}")


if __name__ == "__main__":
    main()
