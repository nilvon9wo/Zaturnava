#!/usr/bin/env bash
# Build and run the UTCP sample bot (samples/UtcpSampleBot) under Linux, from Windows through WSL, passing it the
# arguments:
#
#     wsl -d Ubuntu -- bash scripts/wsl-utcp-sample-bot.sh --server https://zaturnava.runasp.net/
#
# Run it from the repository's root. It builds to artifacts/wsl-utcp-sample-bot/ (git-ignored).
set -u
cd "$(dirname "$0")/.." || exit 1
exec dotnet run --project samples/UtcpSampleBot/UtcpSampleBot.csproj -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-utcp-sample-bot" -- "$@"
