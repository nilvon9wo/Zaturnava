#!/usr/bin/env bash
# Play the game under Linux, from Windows through WSL, in this checkout itself (server on port 5164, page on
# 5165, both opening in a Windows browser, as WSL forwards the ports):
#
#     wsl -d Ubuntu -- bash scripts/wsl-play.sh
#
# Run it from the repository's root. It builds to artifacts/wsl-play/ (git-ignored), apart from the tests' build
# (scripts/wsl-test.sh) and from Windows' bin/ and obj/, so a test run never rebuilds the game beneath you.
set -u
cd "$(dirname "$0")/.." || exit 1
exec python3 scripts/play.py -p:UseArtifactsOutput=true "-p:ArtifactsPath=$PWD/artifacts/wsl-play"
