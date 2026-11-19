#!/usr/bin/env bash
# Train and rate the bots while no one is working, under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-overnight.sh [--cycles N] [--rulesets a,b] [--rounds N] [--games N]
#
# Run it from the repository's root. Each cycle, for each ruleset in turn: rounds of Athena's learning
# (scripts/athena-rounds.py, kept in artifacts/overnight/athena/<ruleset>/, so that each run goes on from the last),
# then an Elo tournament of every bot
# (scripts/rate-bots.py), its ratings written to artifacts/overnight/<started>/<ruleset>-<cycle>.json.
# It goes on for --cycles cycles (0, the default: until stopped with Ctrl+C). Everything it writes is git-ignored.
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1

cycles=0
rulesets="chaturaji-2027,grand-legions-small,grand-legions-medium,grand-legions-large"
rounds=2
games=100
while [ $# -gt 0 ]; do
    case "$1" in
        --cycles) cycles="$2"; shift 2 ;;
        --rulesets) rulesets="$2"; shift 2 ;;
        --rounds) rounds="$2"; shift 2 ;;
        --games) games="$2"; shift 2 ;;
        *) echo "unknown option: $1" >&2; exit 2 ;;
    esac
done

dotnet build src/Bots/Zaturnava.BotRunner -c Release -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-release" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
runner=artifacts/wsl-release/bin/Zaturnava.BotRunner/release/Net.NowhereAtAll.Zaturnava.BotRunner
out="artifacts/overnight/$(date +%Y%m%d-%H%M)"
mkdir -p "$out"

cycle=1
while [ "$cycles" -eq 0 ] || [ "$cycle" -le "$cycles" ]; do
    for ruleset in ${rulesets//,/ }; do
        echo "== cycle $cycle, $ruleset: Athena learns ($rounds rounds of $games games)"
        python3 scripts/athena-rounds.py "$runner" --ruleset "$ruleset" --rounds "$rounds" --games "$games" \
            --folder "artifacts/overnight/athena/$ruleset" \
            || echo "   Athena's rounds failed; going on"
        echo "== cycle $cycle, $ruleset: Elo tournament ($games games)"
        python3 scripts/rate-bots.py "$runner" --ruleset "$ruleset" --games "$games" \
            --out "$out/$ruleset-$cycle.json" \
            || echo "   the tournament failed; going on"
    done
    cycle=$((cycle + 1))
done
echo "== done; ratings in $out"
