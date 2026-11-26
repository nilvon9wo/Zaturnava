#!/usr/bin/env python3
"""Rate the bots: play each pair of them against each other, then fit Elo ratings to the results.

Each pair plays --games games with the first bot's team moving first (A,B,A,B), and as many with the
other's (B,A,B,A), through the BotRunner. A game goes to the team that won it, if it ended so; a game
that ended in a draw is one; a game that reached --max-steps goes to the team with more points, a draw
if level. Ratings are fitted to every result at once
(Bradley-Terry, which Elo's expected score is), with one draw added between each pair, so that a bot
that never loses still gets a finite rating, and anchored with random at 1000, so that a new bot does
not move the others. They are written as JSON, by bot, for src/Bots/Zaturnava.Bots/Ratings/.
With --bot-assembly, bots of one's own (the BotRunner's option of that name) may be among --bots. Without --bots,
every bot the BotRunner says plays the ruleset (its --list bots). A slow bot (SLOW) plays only the other slow ones
and the strongest of the rest (RIVALS), last.
With --out, each pair's result is also kept, as it ends, in <out>.pairs.json; run again with the same --out,
--ruleset, --games and --max-steps, a tournament stopped mid-way plays only the pairs it had not finished; and each
game, as it ends, in <out>.games/, so that a pair stopped mid-way plays only the games it had not.
With --hold-shipped, the bots the server ships rated at the ruleset keep those ratings, and only the others are
fitted, around them, as the server's bot-rating job fits them: so that local games, more of them than the server
plays, rate new bots on the scale it ships (M4); only those fitted are written. With --free, the bots named are
fitted afresh though shipped, to measure them again. With --ship as well, those fitted are merged into the ratings
the server ships (src/Bots/Zaturnava.Bots/Ratings/<ruleset>.json), for the next deploy to rate them so live, and the
measurement is kept, beside every one before it, in docs/bots/rating-history.jsonl (M4, M5).

Usage: rate-bots.py RUNNER [--bots argus,echo,...] [--games 50] [--max-steps 2000]
                           [--ruleset chaturaji-2027] [--out ratings.json] [--jobs N]
                           [--bot-assembly MyBots.dll] [--hold-shipped [--free morpheus,...] [--ship]]
"""

import argparse
import itertools
import json
import math
import os
import subprocess
import sys
import tempfile
import threading
import time
from concurrent.futures import ThreadPoolExecutor

ANCHOR = "random"
ANCHOR_RATING = 1000
SCALE = 400 / math.log(10)
# The bots too slow to play every other (they search, or judge by a network): each plays only the others of them
# and RIVALS, the strongest of the rest, which is enough for one fit of every result to rate them on the same scale
# (2026-11-20: the best bots are rated too, not left out); their pairs are played last, so that a tournament
# stopped early has rated the rest.
SLOW = {"athena", "pythia", "tactician"}
RIVALS = {"sage", "argus", "solomon"}
# How often, in seconds, the games each pair playing has finished are told (2026-11-26: a pair of Sage's, two
# hundred games of a minute, said nothing for hours)
HEARTBEAT = 300
# The pairs playing now: each one's games file, with what it is told as, the games it played before that file, and
# all it is to play
PLAYING = {}
PLAYING_LOCK = threading.Lock()


def known(runner, ruleset):
    """Every bot the BotRunner says plays the ruleset (--list bots), the weak (D2) as well as the strong, and each
    family's where its pieces are to be had (2026-11-20: so that they are rated too)."""
    return subprocess.run([*runner, "--ruleset", ruleset, "--list", "bots"], check=True, capture_output=True,
                          text=True).stdout.split()


def pairings(bots):
    """The pairs to play: every pair of bots, but a slow one only with the other slow ones and the rivals; the
    pairs with a slow bot last."""
    def wanted(pair):
        slow = [bot in SLOW for bot in pair]
        return not any(slow) or all(slow) or any(bot in RIVALS for bot in pair)

    return sorted((pair for pair in itertools.combinations(bots, 2) if wanted(pair)),
                  key=lambda pair: any(bot in SLOW for bot in pair))


def play(runner, ruleset, seats, games, max_steps, told=None, keep=None):
    """Each game the seats' bots play, as the BotRunner logs it: its seats with their scores, and its outcome. While
    they play, the games file is among those PLAYING, as told says, if it says (its name, games before, all). With
    keep, each game is kept in that file as it ends, and only those not kept yet are played (2026-11-26: a pair of
    three hours stopped near its end began again from its first game)."""
    if keep is None:
        with tempfile.NamedTemporaryFile(suffix=".jsonl", delete=False) as out:
            path = out.name
        try:
            run_games(runner, ruleset, seats, games, max_steps, path, told)
            return read_games(path)
        finally:
            os.remove(path)
    part = keep + ".part"
    absorb(part, keep)
    done = len(read_games(keep))
    if done < games:
        name, before, whole = told or ("", 0, games)
        run_games(runner, ruleset, seats, games - done, max_steps, part, (name, before + done, whole))
        absorb(part, keep)
    return read_games(keep)[:games]


def run_games(runner, ruleset, seats, games, max_steps, path, told):
    """The BotRunner plays the games into path, which is among those PLAYING meanwhile, if told says how."""
    if told:
        with PLAYING_LOCK:
            PLAYING[path] = told
    try:
        subprocess.run(
            [*runner, "--ruleset", ruleset, "--bots", ",".join(seats), "--games", str(games),
             "--max-steps", str(max_steps), "--out", path],
            check=True,
        )
    finally:
        with PLAYING_LOCK:
            PLAYING.pop(path, None)


def read_games(path):
    """The games the file holds, each line one; a line cut short, by a run stopped as it wrote it, left out."""
    games = []
    if os.path.exists(path):
        with open(path, encoding="utf-8") as lines:
            for line in lines:
                try:
                    games.append(json.loads(line))
                except ValueError:
                    pass
    return games


def absorb(part, keep):
    """The whole games of a run's file, stopped or not, added to those kept, and the run's file gone."""
    if os.path.exists(part):
        with open(keep, "a", encoding="utf-8", newline="\n") as kept:
            for game in read_games(part):
                kept.write(json.dumps(game) + "\n")
        os.remove(part)


def games_in(path):
    """How many games the file holds so far; none, if it cannot be read."""
    try:
        with open(path, encoding="utf-8") as lines:
            return sum(1 for line in lines if line.strip())
    except OSError:
        return 0


def heartbeat(stopped, finished, total):
    """Every HEARTBEAT seconds, till stopped: the pairs finished, and each pair playing with its games so far."""
    while not stopped.wait(HEARTBEAT):
        with PLAYING_LOCK:
            playing = list(PLAYING.items())
        if playing:
            each = ", ".join(f"{name} {before + games_in(path)}/{whole}"
                             for path, (name, before, whole) in sorted(playing, key=lambda item: item[1][0]))
            print(f"   ... {time.strftime('%H:%M')}, {finished[0]}/{total} pairs done; playing {each}",
                  file=sys.stderr, flush=True)


def results(runner, ruleset, a, b, games, max_steps, kept_games=None):
    """A's wins, losses and draws against B, with each team moving first in half the games, and how long they took;
    each half's games kept in kept_games, a folder, if given, as they end."""
    won = lost = drawn = 0
    started = time.monotonic()
    for half, (seats, a_seats) in enumerate((((a, b, a, b), (0, 2)), ((b, a, b, a), (1, 3)))):
        keep = os.path.join(kept_games, f"{a}-vs-{b}-{half}.jsonl") if kept_games else None
        told = (f"{a} vs {b}", half * games, 2 * games)
        for game in play(runner, ruleset, seats, games, max_steps, told, keep):
            mine, theirs = margin(game, a_seats)
            won += mine > theirs
            lost += mine < theirs
            drawn += mine == theirs
    return won, lost, drawn, time.monotonic() - started


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


def load_kept(path, settings):
    """The pairs' results kept by an earlier run of the same tournament, if any; none, if its settings differ."""
    if not path or not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as kept:
        content = json.load(kept)
    return content["pairs"] if content.get("settings") == settings else {}


def save_kept(path, settings, pairs):
    """The pairs' results so far, written whole each time, so that a stop mid-write loses at most the last."""
    if not path:
        return
    with open(path + ".tmp", "w", encoding="utf-8", newline="\n") as out:
        json.dump({"settings": settings, "pairs": pairs}, out, indent=2)
    os.replace(path + ".tmp", path)


def fit(bots, record):
    """Bradley-Terry strengths by minorization-maximization, a draw counting half a win each way."""
    wins = {bot: 0.0 for bot in bots}
    games = {pair: 0.0 for pair in itertools.combinations(bots, 2)}
    for (a, b), (won, lost, drawn, _) in record.items():
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


ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
HISTORY = os.path.join(ROOT, "docs", "bots", "rating-history.jsonl")


def shipped_path(ruleset):
    """Where the server's ratings at the ruleset are kept."""
    return os.path.join(ROOT, "src", "Bots", "Zaturnava.Bots", "Ratings", ruleset + ".json")


def shipped(ruleset):
    """The ratings the server ships at the ruleset (src/Bots/Zaturnava.Bots/Ratings/), by bot; none, if none."""
    path = shipped_path(ruleset)
    if not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as kept:
        return json.load(kept)


def ship(ruleset, ratings, record, games):
    """The ratings fitted merged into those the server ships at the ruleset, weakest first, so that the next deploy
    rates the bots so, live (M4); and the measurement kept, beside every one before it, in HISTORY (M5): when, at
    what ruleset, how many games a pair, each pair's result, and the ratings fitted."""
    merged = {**shipped(ruleset), **ratings}
    with open(shipped_path(ruleset), "w", encoding="utf-8", newline="\n") as out:
        out.write(json.dumps(dict(sorted(merged.items(), key=lambda rating: rating[1])), indent=2) + "\n")
    measurement = {
        "date": time.strftime("%Y-%m-%d"),
        "ruleset": ruleset,
        "gamesEachWay": games,
        "pairs": {f"{a}|{b}": list(result[:3]) for (a, b), result in record.items()},
        "ratings": ratings,
    }
    with open(HISTORY, "a", encoding="utf-8", newline="\n") as out:
        out.write(json.dumps(measurement) + "\n")


def fit_around(bots, record, held):
    """Bradley-Terry strengths of the bots not held, the held kept at their ratings, as the server's bot-rating job
    fits them (BotRatingFit.Around): one draw added for each bot fitted, against one rated ANCHOR_RATING, rather than
    one between each pair, so that a weak bot is not drawn up toward the strong ones it lost every game to
    (2026-11-26); so that local games rate new bots on the scale the server ships (M4)."""
    free = [bot for bot in bots if bot not in held]
    wins = {bot: 0.5 for bot in free}
    games = {}
    for (a, b), (won, lost, drawn, _) in record.items():
        if a in wins:
            wins[a] += won + drawn / 2
        if b in wins:
            wins[b] += lost + drawn / 2
        games[(a, b)] = won + lost + drawn
    strength = {bot: math.exp((rating - ANCHOR_RATING) / SCALE) for bot, rating in held.items() if bot in bots}
    strength.update({bot: 1.0 for bot in free})
    for _ in range(10000):
        updated = dict(strength)
        for bot in free:
            denominator = 1 / (strength[bot] + 1) + sum(
                n / (strength[bot] + strength[a if b == bot else b])
                for (a, b), n in games.items()
                if bot in (a, b)
            )
            updated[bot] = wins[bot] / denominator
        done = all(abs(updated[bot] - strength[bot]) < 1e-12 * strength[bot] for bot in free)
        strength = updated
        if done:
            break
    return {bot: round(ANCHOR_RATING + SCALE * math.log(strength[bot])) for bot in free}


def main():
    parser = argparse.ArgumentParser(description="Rate the bots, by Elo, from games between each pair.")
    parser.add_argument("runner")
    parser.add_argument("--bots")
    parser.add_argument("--games", type=int, default=50)
    parser.add_argument("--max-steps", type=int, default=2000)
    parser.add_argument("--ruleset", default="chaturaji-2027")
    parser.add_argument("--out")
    parser.add_argument("--jobs", type=int, default=os.cpu_count())
    parser.add_argument("--bot-assembly")
    parser.add_argument("--hold-shipped", action="store_true")
    parser.add_argument("--free")
    parser.add_argument("--ship", action="store_true")
    options = parser.parse_args()
    if options.ship and not options.hold_shipped:
        sys.exit("--ship needs --hold-shipped, so that the bots already shipped keep the scale the server rates on.")
    runner = [options.runner] + (["--bot-assembly", options.bot_assembly] if options.bot_assembly else [])
    bots = options.bots.split(",") if options.bots else known(runner, options.ruleset)
    if ANCHOR not in bots:
        sys.exit(f"The bots must include {ANCHOR}, whose rating anchors the others.")
    pairs = pairings(bots)
    # M2: each pair's result, as it ends, is kept beside --out, so that a tournament stopped mid-way goes on from
    # the pairs already played when run again with the same --out, --games and --max-steps
    kept_path = options.out + ".pairs.json" if options.out else None
    settings = f"{options.ruleset}/{options.games}/{options.max_steps}"
    # and each pair's games, as each ends, so that a pair stopped mid-way goes on from the games it had played
    kept_games = f"{options.out}.games/{options.ruleset}-{options.games}-{options.max_steps}" if options.out else None
    if kept_games:
        os.makedirs(kept_games, exist_ok=True)
    kept = load_kept(kept_path, settings)
    lock = threading.Lock()

    finished = [sum(f"{a}|{b}" in kept for a, b in pairs)]
    if finished[0]:
        print(f"   {finished[0]} of {len(pairs)} pairs kept from before", file=sys.stderr, flush=True)

    def played(pair):
        key = f"{pair[0]}|{pair[1]}"
        if key in kept:
            return tuple(kept[key])
        result = results(runner, options.ruleset, pair[0], pair[1], options.games, options.max_steps, kept_games)
        with lock:
            kept[key] = list(result)
            save_kept(kept_path, settings, kept)
            # each pair as it ends (2026-11-25: a tournament silent for twelve hours looked frozen)
            finished[0] += 1
            won, lost, drawn, seconds = result
            print(f"   {finished[0]}/{len(pairs)} {pair[0]} vs {pair[1]}: won {won}, lost {lost}, drawn {drawn},"
                  f" in {seconds / 60:.0f} min", file=sys.stderr, flush=True)
        return result

    stopped = threading.Event()
    threading.Thread(target=heartbeat, args=(stopped, finished, len(pairs)), daemon=True).start()
    try:
        with ThreadPoolExecutor(options.jobs) as pool:
            record = dict(zip(pairs, pool.map(played, pairs)))
    finally:
        stopped.set()
    for (a, b), (won, lost, drawn, seconds) in record.items():
        print(f"{a} vs {b}: won {won}, lost {lost}, drawn {drawn}, in {seconds:.0f} s", file=sys.stderr)
    freed = set(options.free.split(",")) if options.free else set()
    held = {bot: rating for bot, rating in shipped(options.ruleset).items() if bot not in freed} \
        if options.hold_shipped else {}
    ratings = fit_around(bots, record, held) if held else fit(bots, record)
    if options.ship:
        ship(options.ruleset, ratings, record, options.games)
    text = json.dumps(dict(sorted(ratings.items(), key=lambda rating: rating[1])), indent=2) + "\n"
    if options.out:
        with open(options.out, "w", encoding="utf-8", newline="\n") as out:
            out.write(text)
    else:
        print(text, end="")


if __name__ == "__main__":
    main()
