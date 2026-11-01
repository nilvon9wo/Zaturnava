#!/usr/bin/env bash
# Build and run the sample bot (samples/SampleBot) under Linux, from Windows through WSL, passing it the arguments:
#
#     wsl -d Ubuntu -- bash scripts/wsl-sample-bot.sh --server https://zaturnava.runasp.net/
#
# Run it from the repository's root. It builds to artifacts/wsl-sample-bot/ (git-ignored).
set -u
cd "$(dirname "$0")/.." || exit 1
exec dotnet run --project samples/SampleBot/SampleBot.csproj -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-sample-bot" -- "$@"
