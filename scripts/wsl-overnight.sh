#!/usr/bin/env bash
# Train and rate the bots while no one is working, under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-overnight.sh [--cycles N] [--rulesets a,b] [--rounds N] [--games N]
#                                                    [--continue artifacts/overnight/<started>]
#
# With --continue, it writes into that earlier run's folder instead of a new one, and each tournament it had
# stopped mid-way goes on from the pairs of bots it had finished (M2), numbered on from the cycle it stopped in.
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
out=""
while [ $# -gt 0 ]; do
    case "$1" in
        --cycles) cycles="$2"; shift 2 ;;
        --rulesets) rulesets="$2"; shift 2 ;;
        --rounds) rounds="$2"; shift 2 ;;
        --games) games="$2"; shift 2 ;;
        --continue) out="${2%/}"; shift 2 ;;
        *) echo "unknown option: $1" >&2; exit 2 ;;
    esac
done

dotnet build src/Bots/Zaturnava.BotRunner -c Release -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-release" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
runner=artifacts/wsl-release/bin/Zaturnava.BotRunner/release/Net.NowhereAtAll.Zaturnava.BotRunner
cycle=1
if [ -z "$out" ]; then
    out="artifacts/overnight/$(date +%Y%m%d-%H%M)"
else
    # on from the last cycle any tournament had begun: its kept pairs, if it stopped mid-way, are played no more
    for kept in "$out"/*-*.json.pairs.json "$out"/*-*.json; do
        [ -e "$kept" ] || continue
        number="${kept##*-}"
        number="${number%%.*}"
        [ "$number" -gt "$cycle" ] 2>/dev/null && cycle="$number"
    done
fi
mkdir -p "$out"

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
