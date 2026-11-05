#!/usr/bin/env bash
# Build and run the HTTP API sample bot (samples/HttpApiSampleBot) under Linux, from Windows through WSL, passing it
# the arguments:
#
#     wsl -d Ubuntu -- bash scripts/wsl-http-api-sample-bot.sh --server https://zaturnava.runasp.net/
#
# Run it from the repository's root. It builds to artifacts/wsl-http-api-sample-bot/ (git-ignored).
set -u
cd "$(dirname "$0")/.." || exit 1
exec dotnet run --project samples/HttpApiSampleBot/HttpApiSampleBot.csproj -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-http-api-sample-bot" -- "$@"
