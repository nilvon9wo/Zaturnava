#!/usr/bin/env python3
"""Rate the bots: play each pair of them against each other, then fit Elo ratings to the results.

Each pair plays --games games with the first bot's team moving first (A,B,A,B), and as many with the
other's (B,A,B,A), through the BotRunner. A game goes to the team that won it, if it ended so; a game
that ended in a draw is one; a game that reached --max-steps goes to the team with more points, a draw
if level. Ratings are fitted to every result at once
(Bradley-Terry, which Elo's expected score is), with one draw added between each pair, so that a bot
that never loses still gets a finite rating, and anchored with random at 1000, so that a new bot does
not move the others. They are written as JSON, by bot, for src/Bots/Zaturnava.Bots/Ratings/.
With --bot-assembly, bots of one's own (the BotRunner's option of that name) may be among --bots.

Usage: rate-bots.py RUNNER [--bots argus,midas,random,sage,solomon] [--games 50] [--max-steps 2000]
                           [--ruleset chaturaji-2027] [--out ratings.json] [--jobs N]
                           [--bot-assembly MyBots.dll]
"""

import argparse
import itertools
import json
import math
import os
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor

ANCHOR = "random"
ANCHOR_RATING = 1000
SCALE = 400 / math.log(10)


def play(runner, ruleset, seats, games, max_steps):
    """Each game the seats' bots play, as the BotRunner logs it: its seats with their scores, and its outcome."""
    with tempfile.NamedTemporaryFile(suffix=".jsonl", delete=False) as out:
        path = out.name
    try:
        subprocess.run(
            [*runner, "--ruleset", ruleset, "--bots", ",".join(seats), "--games", str(games),
             "--max-steps", str(max_steps), "--out", path],
            check=True,
        )
        with open(path, encoding="utf-8") as lines:
            return [json.loads(line) for line in lines if line.strip()]
    finally:
        os.remove(path)


def results(runner, ruleset, a, b, games, max_steps):
    """A's wins, losses and draws against B, with each team moving first in half the games."""
    won = lost = drawn = 0
    for seats, a_seats in (((a, b, a, b), (0, 2)), ((b, a, b, a), (1, 3))):
        for game in play(runner, ruleset, seats, games, max_steps):
            mine, theirs = margin(game, a_seats)
            won += mine > theirs
            lost += mine < theirs
            drawn += mine == theirs
    return won, lost, drawn


def margin(game, seats):
    """How the seats' team came out, against the other: by who won, if the game ended so; else by points."""
    outcome = game["outcome"]
    if outcome and outcome["winners"]:
        return (1, 0) if outcome["winners"][0] in seats else (0, 1)
    if outcome:
        return 0, 0
    mine = sum(seat["score"] for seat in game["seats"] if seat["id"] in seats)
    theirs = sum(seat["score"] for seat in game["seats"] if seat["id"] not in seats)
    return mine, theirs


def fit(bots, record):
    """Bradley-Terry strengths by minorization-maximization, a draw counting half a win each way."""
    wins = {bot: 0.0 for bot in bots}
    games = {pair: 0.0 for pair in itertools.combinations(bots, 2)}
    for (a, b), (won, lost, drawn) in record.items():
        wins[a] += won + (drawn + 1) / 2
        wins[b] += lost + (drawn + 1) / 2
        games[(a, b)] = won + lost + drawn + 1
    strength = {bot: 1.0 for bot in bots}
    for _ in range(10000):
        updated = {}
        for bot in bots:
            denominator = sum(
                n / (strength[bot] + strength[a if b == bot else b])
                for (a, b), n in games.items()
                if bot in (a, b)
            )
            updated[bot] = wins[bot] / denominator
        anchor = updated[ANCHOR]
        updated = {bot: value / anchor for bot, value in updated.items()}
        done = all(abs(updated[bot] - strength[bot]) < 1e-12 for bot in bots)
        strength = updated
        if done:
            break
    return {bot: round(ANCHOR_RATING + SCALE * math.log(strength[bot])) for bot in bots}


def main():
    parser = argparse.ArgumentParser(description="Rate the bots, by Elo, from games between each pair.")
    parser.add_argument("runner")
    parser.add_argument("--bots", default="argus,midas,random,sage,solomon")
    parser.add_argument("--games", type=int, default=50)
    parser.add_argument("--max-steps", type=int, default=2000)
    parser.add_argument("--ruleset", default="chaturaji-2027")
    parser.add_argument("--out")
    parser.add_argument("--jobs", type=int, default=os.cpu_count())
    parser.add_argument("--bot-assembly")
    options = parser.parse_args()
    runner = [options.runner] + (["--bot-assembly", options.bot_assembly] if options.bot_assembly else [])
    bots = options.bots.split(",")
    if ANCHOR not in bots:
        sys.exit(f"The bots must include {ANCHOR}, whose rating anchors the others.")
    pairs = list(itertools.combinations(bots, 2))
    with ThreadPoolExecutor(options.jobs) as pool:
        played = pool.map(
            lambda pair: results(runner, options.ruleset, pair[0], pair[1], options.games, options.max_steps),
            pairs,
        )
        record = dict(zip(pairs, played))
    for (a, b), (won, lost, drawn) in record.items():
        print(f"{a} vs {b}: won {won}, lost {lost}, drawn {drawn}", file=sys.stderr)
    ratings = fit(bots, record)
    text = json.dumps(dict(sorted(ratings.items(), key=lambda rating: rating[1])), indent=2) + "\n"
    if options.out:
        with open(options.out, "w", encoding="utf-8", newline="\n") as out:
            out.write(text)
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
