"""Whether the dice of finished games came up fairly, for each kind of player who rolled them.

    py -3 scripts/dice-audit.py artifacts/live-history.json

Reads the history of every finished game (GET api/history, saved to a file) and counts each face rolled, by who
held the seat it was rolled for: each bot by its id, people as "person". For each, a chi-square test of the faces
against fair dice; and, for the faces that matter most in Chaturaji (the user's report of 2026-11-20: Sage
"gets lucky rolls far too often"), how often a seat's two dice came up a double.
"""

import json
import math
import sys
from collections import Counter, defaultdict


def chi_square_p(counts, sides):
    """The chance that fair dice would stray at least this far from even, by the chi-square test."""
    total = sum(counts[face] for face in range(1, sides + 1))
    if total == 0:
        return float("nan")
    expected = total / sides
    statistic = sum((counts[face] - expected) ** 2 / expected for face in range(1, sides + 1))
    return upper_gamma_ratio((sides - 1) / 2, statistic / 2)


def upper_gamma_ratio(a, x):
    """Q(a, x), the regularized upper incomplete gamma function, by series or continued fraction."""
    if x <= 0:
        return 1.0
    if x < a + 1:
        term = total = 1 / a
        n = a
        for _ in range(500):
            n += 1
            term *= x / n
            total += term
        return 1 - total * math.exp(-x + a * math.log(x) - math.lgamma(a))
    b, c, d = x + 1 - a, 1 / 1e-300, 1 / (x + 1 - a)
    h = d
    for i in range(1, 500):
        an = -i * (i - a)
        b += 2
        d = an * d + b
        d = 1 / (d if abs(d) > 1e-300 else 1e-300)
        c = b + an / c
        c = c if abs(c) > 1e-300 else 1e-300
        h *= d * c
    return math.exp(-x + a * math.log(x) - math.lgamma(a)) * h


def controller_of(seat):
    """Who held a seat: a bot by its id, else a person."""
    for key in ("player", "botId", "bot"):
        if isinstance(seat, dict) and seat.get(key):
            return str(seat[key]).lower()
    return "person"


def main():
    with open(sys.argv[1], encoding="utf-8") as file:
        games = json.load(file)
    if isinstance(games, dict):
        games = next(value for value in games.values() if isinstance(value, list))
    first = games[0]
    print("game keys:", sorted(first.keys()))
    faces = defaultdict(Counter)
    doubles = Counter()
    rolls = Counter()
    sides = 0
    for game in games:
        seats = game.get("seats") or game.get("controllers") or []
        owners = {index: controller_of(seat) for index, seat in enumerate(seats)}
        for action in game.get("actions", []):
            if action.get("kind") != "dice":
                continue
            arguments = action["arguments"]
            who = owners.get(int(arguments["actor"]), "unknown")
            values = [int(value) for value in str(arguments["values"]).split(",") if value]
            sides = max([sides, *values])
            faces[who].update(values)
            if len(values) == 2:
                rolls[who] += 1
                doubles[who] += values[0] == values[1]
    print(f"{len(games)} games; dice of {sides} sides")
    print(f"{'who':<14}{'faces':>8}  " + "".join(f"{face:>7}" for face in range(1, sides + 1)) + "   p fair  doubles")
    for who in sorted(faces, key=lambda name: -sum(faces[name].values())):
        counts = faces[who]
        total = sum(counts.values())
        shares = "".join(f"{counts[face] / total:>7.3f}" for face in range(1, sides + 1))
        double = f"{doubles[who]}/{rolls[who]} = {doubles[who] / rolls[who]:.3f}" if rolls[who] else "-"
        print(f"{who:<14}{total:>8}  {shares}  {chi_square_p(counts, sides):>7.3f}  {double}")
    print(f"fair: each face {1 / sides:.3f}; doubles {1 / sides:.3f}")


if __name__ == "__main__":
    main()
