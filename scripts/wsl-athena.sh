#!/usr/bin/env bash
# Rounds of Athena's learning under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-athena.sh [athena-rounds.py options, but the runner]
#
# Run it from the repository's root. It builds the BotRunner for Release to artifacts/wsl-release/ (git-ignored),
# then runs scripts/athena-rounds.py with it and whatever options it is given, such as --rounds and --games.
# Each round's network, games and result are kept in artifacts/athena/ (git-ignored).
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
dotnet build src/Bots/Zaturnava.BotRunner -c Release -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-release" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
exec python3 scripts/athena-rounds.py \
    artifacts/wsl-release/bin/Zaturnava.BotRunner/release/Net.NowhereAtAll.Zaturnava.BotRunner "$@"
