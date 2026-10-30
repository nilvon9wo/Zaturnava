#!/usr/bin/env bash
# Rate the bots under Linux, from Windows through WSL, in this checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-rate.sh [rate-bots.py options, but the runner]
#
# Run it from the repository's root. It builds the BotRunner for Release to artifacts/wsl-release/ (git-ignored),
# then runs scripts/rate-bots.py with it and whatever options it is given, such as --bots and --games.
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
dotnet build src/Bots/Zaturnava.BotRunner -c Release -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-release" -nologo 2>&1 | grep -E "error|Build succeeded" | sort -u || exit 1
exec python3 scripts/rate-bots.py \
    artifacts/wsl-release/bin/Zaturnava.BotRunner/release/Net.NowhereAtAll.Zaturnava.BotRunner "$@"
