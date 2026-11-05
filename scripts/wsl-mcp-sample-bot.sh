#!/usr/bin/env bash
# Build and run the MCP sample bot (samples/McpSampleBot) under Linux, from Windows through WSL, passing it the
# arguments:
#
#     wsl -d Ubuntu -- bash scripts/wsl-mcp-sample-bot.sh --server https://zaturnava.runasp.net/
#
# Run it from the repository's root. It builds to artifacts/wsl-mcp-sample-bot/ (git-ignored).
set -u
cd "$(dirname "$0")/.." || exit 1
exec dotnet run --project samples/McpSampleBot/McpSampleBot.csproj -p:UseArtifactsOutput=true \
    "-p:ArtifactsPath=$PWD/artifacts/wsl-mcp-sample-bot" -- "$@"
