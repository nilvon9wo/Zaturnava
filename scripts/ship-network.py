"""Ship a trained network as the one Athena and Pythia play by:

    py -3 scripts/ship-network.py artifacts/athena-board/best.json

Checks the file is a network (format, ruleset, inputs, hidden), then copies it, unchanged, to
src/Bots/Zaturnava.Bots/Networks/RULESET.json, the embedded resource ValueNetworks.Learned reads. The bots fit an
encoder to the network by its inputs, so a network that sees the board replaces one that does not as it is.
"""

import argparse
import json
import os
import shutil

NETWORKS = os.path.join("src", "Bots", "Zaturnava.Bots", "Networks")
FIELDS = ("format", "rulesetId", "inputs", "hidden", "inputWeights", "outputWeights")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("network", help="the trained network's JSON, as BotRunner --learn writes it")
    arguments = parser.parse_args()
    with open(arguments.network, encoding="utf-8") as source:
        network = json.load(source)
    missing = [field for field in FIELDS if field not in network]
    if missing:
        raise SystemExit(f"{arguments.network} is not a network: no {', '.join(missing)}")
    target = os.path.join(NETWORKS, network["rulesetId"] + ".json")
    replaced = None
    if os.path.exists(target):
        with open(target, encoding="utf-8") as old:
            replaced = json.load(old).get("inputs")
    shutil.copyfile(arguments.network, target)
    print(f"{target}: {network['inputs']} inputs, {network['hidden']} hidden (was {replaced} inputs)")


if __name__ == "__main__":
    main()
