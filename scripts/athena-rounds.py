#!/usr/bin/env python3
"""Rounds of Athena's learning, each one command's work, kept in --folder (default artifacts/athena, git-ignored).

Each round:
1. learns: a network goes on from the last round's (round-N.json), or starts judging as Sage does, and learns
   from every game the rounds have played (games-*.jsonl) and any --seed files of games, by temporal
   differences (--lambda), in --epochs passes, through the BotRunner's --learn;
2. measures: Athena, playing by the new network, plays Sage --games games with her team moving first
   (athena,sage,athena,sage) and as many with Sage's, in --jobs processes at once; those games are kept,
   for the rounds after it to learn from;
3. tells how she did: won, lost and drawn (a game that reached --max-steps going to the team with more
   points), and her share, a draw counting half. Each round is a line of rounds.jsonl; the network with the
   best share so far is copied to best.json. A share of at least 55% over 400 games is the bar for calling
   her stronger than Sage.

Usage: athena-rounds.py RUNNER [--rounds 1] [--games 200] [--epochs 4] [--lambda 0.8] [--rate 0.1]
                               [--hidden 32] [--seed games.jsonl,more.jsonl] [--max-steps 2000] [--jobs N]
                               [--folder artifacts/athena]
"""

import argparse
import glob
import importlib
import json
import os
import shutil
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor

rate_bots = importlib.import_module("rate-bots")

BAR = 0.55
SIDES = ((("athena", "sage", "athena", "sage"), (0, 2)), (("sage", "athena", "sage", "athena"), (1, 3)))


def rounds_done(folder):
    """How many rounds the folder holds the networks of."""
    return len(glob.glob(os.path.join(folder, "round-*.json")))


def learn(runner, folder, number, options):
    """The round's network, learned from every game played so far, going on from the last round's."""
    games = sorted(glob.glob(os.path.join(folder, "games-*.jsonl")))
    games += [seed for seed in options.seed.split(",") if seed] if options.seed else []
    if not games:
        games = [os.path.join(folder, "games-0.jsonl")]
        open(games[0], "w", encoding="utf-8").close()
    network = os.path.join(folder, f"round-{number}.json")
    previous = os.path.join(folder, f"round-{number - 1}.json")
    start = ["--from", previous] if os.path.exists(previous) else ["--hidden", str(options.hidden)]
    subprocess.run(
        [*runner, "--learn", ",".join(games), *start, "--epochs", str(options.epochs),
         "--lambda", str(options.lambda_), "--rate", str(options.rate), "--out", network],
        check=True,
    )
    return network


def play(runner, network, seats, games, max_steps, out):
    """So many games of the seats' bots, Athena playing by the network, written to the file."""
    subprocess.run(
        [*runner, "--network", network, "--bots", ",".join(seats), "--games", str(games),
         "--max-steps", str(max_steps), "--out", out],
        check=True,
    )


def measure(runner, folder, number, network, options):
    """Athena's wins, losses and draws against Sage, each team moving first in half the games; the games kept."""
    parts = []
    for side, (seats, _) in enumerate(SIDES):
        share, rest = divmod(options.games, options.jobs)
        for job in range(options.jobs):
            games = share + (job < rest)
            if games:
                parts.append((seats, games, os.path.join(folder, f"part-{number}-{side}-{job}.jsonl")))
    with ThreadPoolExecutor(len(parts)) as pool:
        list(pool.map(lambda part: play(runner, network, part[0], part[1], options.max_steps, part[2]), parts))
    won = lost = drawn = 0
    with open(os.path.join(folder, f"games-{number}.jsonl"), "w", encoding="utf-8", newline="\n") as kept:
        for seats, _, path in parts:
            athena = SIDES[0][1] if seats == SIDES[0][0] else SIDES[1][1]
            with open(path, encoding="utf-8") as lines:
                for line in lines:
                    if not line.strip():
                        continue
                    kept.write(line if line.endswith("\n") else line + "\n")
                    mine, theirs = rate_bots.margin(json.loads(line), athena)
                    won += mine > theirs
                    lost += mine < theirs
                    drawn += mine == theirs
            os.remove(path)
    return won, lost, drawn


def best_share(folder):
    """The best share any round so far has had, or nothing."""
    path = os.path.join(folder, "rounds.jsonl")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as lines:
        return max((json.loads(line)["share"] for line in lines if line.strip()), default=None)


def main():
    parser = argparse.ArgumentParser(description="Rounds of Athena's learning, measured against Sage.")
    parser.add_argument("runner")
    parser.add_argument("--rounds", type=int, default=1)
    parser.add_argument("--games", type=int, default=200)
    parser.add_argument("--epochs", type=int, default=4)
    parser.add_argument("--lambda", dest="lambda_", type=float, default=0.8)
    parser.add_argument("--rate", type=float, default=0.1)
    parser.add_argument("--hidden", type=int, default=32)
    parser.add_argument("--seed")
    parser.add_argument("--max-steps", type=int, default=2000)
    parser.add_argument("--jobs", type=int, default=os.cpu_count())
    parser.add_argument("--folder", default=os.path.join("artifacts", "athena"))
    options = parser.parse_args()
    os.makedirs(options.folder, exist_ok=True)
    runner = [options.runner]
    for _ in range(options.rounds):
        number = rounds_done(options.folder) + 1
        network = learn(runner, options.folder, number, options)
        won, lost, drawn = measure(runner, options.folder, number, network, options)
        share = (won + drawn / 2) / max(1, won + lost + drawn)
        best = best_share(options.folder)
        record = {"round": number, "won": won, "lost": lost, "drawn": drawn, "share": round(share, 4),
                  "lambda": options.lambda_, "epochs": options.epochs, "rate": options.rate}
        with open(os.path.join(options.folder, "rounds.jsonl"), "a", encoding="utf-8", newline="\n") as log:
            log.write(json.dumps(record) + "\n")
        if best is None or share > best:
            shutil.copyfile(network, os.path.join(options.folder, "best.json"))
        verdict = "meets the bar" if share >= BAR and won + lost + drawn >= 400 else "below the bar"
        print(f"round {number}: won {won}, lost {lost}, drawn {drawn}; share {share:.1%}, {verdict}", flush=True)


if __name__ == "__main__":
    sys.exit(main())
