"""Check the dice of the server's finished games for anything but fair, independent rolls.

    py -3 scripts/dice-check.py [--server https://zaturnava.runasp.net/] [--ruleset chaturaji-2027]

Reads every finished game's history (api/history), whose actions record each roll ("dice", values "3,5"), and tells:
how often each face came up, against the even share, with a chi-square test; how often each pair of a two-dice roll
came up (do the two dice depend on each other?); how often doubles came; and whether any games share the same opening
rolls, as games rolled from one fixed seed would.
"""

import argparse
import collections
import json
import math
import urllib.request

# chi-square critical values at 5% and 1%, by degrees of freedom
CRITICAL = {5: (11.07, 15.09), 35: (49.80, 57.34)}


def histories(server):
    request = urllib.request.Request(server.rstrip("/") + "/api/history", headers={"User-Agent": "dice-check"})
    with urllib.request.urlopen(request, timeout=120) as response:
        return json.load(response)


def rolls_of(game):
    for action in game["actions"]:
        if action["kind"] == "dice":
            yield [int(value) for value in action["arguments"]["values"].split(",")]


def rollers_of(game):
    """Each roll with who rolled it: a person, or a bot (the seat's player, as the history names it)."""
    players = {str(seat["id"]): seat["player"] for seat in game["seats"]}
    for action in game["actions"]:
        if action["kind"] == "dice":
            roller = "person" if players.get(action["arguments"]["actor"]) == "person" else "bot"
            yield roller, [int(value) for value in action["arguments"]["values"].split(",")]


def chi_square(counts, expected):
    return sum((count - expected) ** 2 / expected for count in counts)


def verdict(statistic, freedom):
    five, one = CRITICAL[freedom]
    if statistic > one:
        return "UNLIKELY by chance (beyond 1%)"
    if statistic > five:
        return "doubtful (beyond 5%; expect this 1 time in 20 by chance)"
    return "fair"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="https://zaturnava.runasp.net/")
    parser.add_argument("--ruleset", default="chaturaji-2027")
    parser.add_argument("--sides", type=int, default=6)
    arguments = parser.parse_args()

    games = [game for game in histories(arguments.server) if game["rulesetId"] == arguments.ruleset]
    sides = arguments.sides
    faces = collections.Counter()
    pairs = collections.Counter()
    openings = collections.defaultdict(list)
    for game in games:
        rolls = list(rolls_of(game))
        for roll in rolls:
            faces.update(roll)
        doubles = [roll for roll in rolls if len(roll) == 2]
        for roll in doubles:
            pairs[tuple(roll)] += 1
        if len(rolls) >= 8:
            openings[tuple(tuple(roll) for roll in rolls[:8])].append(game.get("gameId"))

    total = sum(faces.values())
    print(f"{len(games)} finished {arguments.ruleset} games, {total} dice rolled")
    if total == 0:
        return
    print()
    print("face  count  share   even share")
    for face in range(1, sides + 1):
        print(f"{face:>4}  {faces[face]:>5}  {faces[face] / total:6.1%}  {1 / sides:6.1%}")
    statistic = chi_square([faces[face] for face in range(1, sides + 1)], total / sides)
    print(f"chi-square {statistic:.2f} on {sides - 1} degrees of freedom: {verdict(statistic, sides - 1)}")

    rolled = sum(pairs.values())
    if rolled and sides == 6:
        print()
        statistic = chi_square(
            [pairs[(first, second)] for first in range(1, 7) for second in range(1, 7)], rolled / 36
        )
        print(f"{rolled} two-dice rolls; pairs: chi-square {statistic:.2f} on 35 degrees of freedom: "
              f"{verdict(statistic, 35)}")
        doubles = sum(pairs[(face, face)] for face in range(1, 7))
        print(f"doubles {doubles} ({doubles / rolled:.1%}; fair dice: 16.7%)")
        # the user, 2026-11-28: "it seems like I'm the only one who can never roll doubles in this game"
        print()
        print("rolled by  two-dice rolls  doubles  share   chance of so few or fewer, or so many or more")
        by_roller = collections.defaultdict(lambda: [0, 0])
        for game in games:
            for roller, roll in rollers_of(game):
                if len(roll) == 2:
                    by_roller[roller][0] += 1
                    by_roller[roller][1] += roll[0] == roll[1]
        for roller, (count, double) in sorted(by_roller.items()):
            print(f"{roller:>9}  {count:>14}  {double:>7}  {double / count:6.1%}  {tail(count, double, 1 / 6):.1%}")


def tail(count, hits, chance):
    """How likely so few hits, or fewer, if below the expected count; so many, or more, if above: one-sided,
    binomial."""
    def probability(k):
        return math.exp(
            math.lgamma(count + 1) - math.lgamma(k + 1) - math.lgamma(count - k + 1)
            + k * math.log(chance) + (count - k) * math.log(1 - chance)
        )
    expected = count * chance
    ks = range(0, hits + 1) if hits <= expected else range(hits, count + 1)
    return min(1.0, sum(probability(k) for k in ks))

    shared = [ids for ids in openings.values() if len(ids) > 1]
    print()
    print(f"games sharing their first 8 rolls: {sum(len(ids) for ids in shared)}"
          + (f" ({shared})" if shared else " (none: no sign of a fixed seed)"))


if __name__ == "__main__":
    main()
