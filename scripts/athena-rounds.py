#!/usr/bin/env python3
"""Rounds of Athena's learning at --ruleset (default chaturaji-2027), each one command's work, kept in --folder
(default artifacts/athena for Chaturaji 2027, else artifacts/athena-RULESET; git-ignored).

Each round:
1. learns: a network goes on from the last round's (round-N.json), or starts judging as Sage does, and learns
   from every game the rounds have played (games-*.jsonl) and any --seed files of games (each may be a pattern,
   such as artifacts/athena/games-*.jsonl, or a folder, for its games-*.jsonl, else all its .jsonl), by temporal differences (--lambda), in --epochs passes, through the
   BotRunner's --learn; a network started afresh sees the board as --view says: summed up, or square by square
   too (board);
2. measures: Athena, playing by the new network, plays Sage --games games with her team moving first
   (athena,sage,athena,sage) and as many with Sage's, in --jobs processes at once; those games are kept,
   for the rounds after it to learn from;
3. tells how she did: won, lost and drawn (a game that reached --max-steps going to the team with more
   points), and her share, a draw counting half. Each round is a line of rounds.jsonl; the network with the
   best share so far is copied to best.json. A share of at least 55% over 400 games is the bar for calling
   her stronger than Sage.

Usage: athena-rounds.py RUNNER [--rounds 1] [--games 200] [--epochs 4] [--lambda 0.8] [--rate 0.1]
                               [--hidden 32] [--view summed|board] [--seed games.jsonl,more-*.jsonl]
                               [--max-steps 2000] [--jobs N]
                               [--ruleset chaturaji-2027] [--folder artifacts/athena]
"""

import argparse
import glob
import importlib
import json
import os
import shutil
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor, wait

rate_bots = importlib.import_module("rate-bots")

BAR = 0.55
HEARTBEAT = 30
SIDES = ((("athena", "sage", "athena", "sage"), (0, 2)), (("sage", "athena", "sage", "athena"), (1, 3)))
CHATURAJI = "chaturaji-2027"


def folder_for(ruleset):
    """Where a ruleset's rounds are kept unless --folder says: artifacts/athena for Chaturaji 2027, as ever."""
    return os.path.join("artifacts", "athena" if ruleset == CHATURAJI else f"athena-{ruleset}")


def rounds_done(folder):
    """How many rounds the folder holds the networks of: an empty file, left by a run stopped as it wrote
    (2026-11-20: round-5.json, which every night after failed to go on from), is no round's, and is learned again."""
    return len([path for path in glob.glob(os.path.join(folder, "round-*.json")) if os.path.getsize(path) > 0])


def learn(runner, folder, number, options):
    """The round's network, learned from every game played so far, going on from the last round's."""
    games = sorted(glob.glob(os.path.join(folder, "games-*.jsonl")))
    seeds = [seed for seed in options.seed.split(",") if seed] if options.seed else []
    games += [path for seed in seeds for path in seed_files(seed)]
    if not games:
        games = [os.path.join(folder, "games-0.jsonl")]
        open(games[0], "w", encoding="utf-8").close()
    network = os.path.join(folder, f"round-{number}.json")
    previous = os.path.join(folder, f"round-{number - 1}.json")
    start = (["--from", previous] if os.path.exists(previous)
             else ["--hidden", str(options.hidden), "--view", options.view])
    # written aside, then put in place whole, so that a run stopped as it writes leaves no half a network
    writing = network + ".part"
    subprocess.run(
        [*runner, "--learn", ",".join(games), *start, "--epochs", str(options.epochs),
         "--lambda", str(options.lambda_), "--rate", str(options.rate), "--out", writing],
        check=True,
    )
    os.replace(writing, network)
    return network


def seed_files(seed):
    """The files of games a --seed names: a folder's games-*.jsonl, or, if it has none, every .jsonl in it (a
    tournament's, named by its pairings); a pattern's matches; or the file itself. (A pattern given through
    `wsl -- bash` is expanded by the shell on the way, too long a command line for a tournament's 476 files.)"""
    if os.path.isdir(seed):
        return sorted(glob.glob(os.path.join(seed, "games-*.jsonl"))) or sorted(
            glob.glob(os.path.join(seed, "*.jsonl"))
        )
    return sorted(glob.glob(seed)) or [seed]


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
    total = sum(games for _, games, _ in parts)
    print(f"round {number}: Athena plays Sage {total} games, in {len(parts)} processes", flush=True)
    started = time.monotonic()
    with ThreadPoolExecutor(len(parts)) as pool:
        running = [pool.submit(play, runner, network, seats, games, options.max_steps, out)
                   for seats, games, out in parts]
        while not wait(running, timeout=HEARTBEAT).done == set(running):
            minutes = (time.monotonic() - started) / 60
            print(f"  ... {played(parts)} of {total} games played, {minutes:.0f} min so far", flush=True)
        for future in running:
            future.result()
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


def played(parts):
    """How many games the parts' files hold so far."""
    count = 0
    for _, _, path in parts:
        if os.path.exists(path):
            with open(path, encoding="utf-8") as lines:
                count += sum(1 for line in lines if line.strip())
    return count


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
    parser.add_argument("--view", choices=("summed", "board"), default="summed")
    parser.add_argument("--seed")
    parser.add_argument("--max-steps", type=int, default=2000)
    parser.add_argument("--jobs", type=int, default=os.cpu_count())
    parser.add_argument("--ruleset", default=CHATURAJI)
    parser.add_argument("--folder")
    options = parser.parse_args()
    options.folder = options.folder or folder_for(options.ruleset)
    os.makedirs(options.folder, exist_ok=True)
    runner = [options.runner, "--ruleset", options.ruleset]
    for _ in range(options.rounds):
        number = rounds_done(options.folder) + 1
        print(f"round {number}: Athena learns from the games played so far", flush=True)
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
