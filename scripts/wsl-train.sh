#!/usr/bin/env bash
# Tune Sage's weights for a ruleset under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-train.sh --ruleset grand-legions-medium --train 4 --games 40 \
#         --out src/Bots/Zaturnava.Bots/Weights/grand-legions-medium.json
#
# Run it from the repository's root. It builds the BotRunner for Release to artifacts/wsl-release/ (git-ignored),
# then runs it with whatever options it is given (SelfPlayOptions): with --train, it tunes Sage's weights from
# what she has learned for the ruleset so far, and writes them where --out says.
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
dotnet build src/Bots/Zaturnava.BotRunner -c Release -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-release" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
exec artifacts/wsl-release/bin/Zaturnava.BotRunner/release/Net.NowhereAtAll.Zaturnava.BotRunner "$@"
