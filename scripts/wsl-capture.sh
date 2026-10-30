#!/usr/bin/env bash
# Pictures and a video of the page for its itch.io listing, under Linux, from Windows through WSL, in this
# checkout itself:
#
#     wsl -d Ubuntu -- bash scripts/wsl-capture.sh [capture.py options, such as --width 1100 --folder ...]
#
# Run it from the repository's root, with the game running (scripts/wsl-play.sh) and Playwright and ffmpeg
# installed as scripts/capture.py says. It runs scripts/capture.py with whatever options it is given, then
# tells what the video holds (a picture and a sound, and how long each runs).
set -u -o pipefail
cd "$(dirname "$0")/.." || exit 1
folder=artifacts/media
for option in "$@"; do
    [ "${previous:-}" = "--folder" ] && folder=$option
    previous=$option
done
artifacts/tools/venv/bin/python scripts/capture.py "$@" || exit 1
ffprobe -v error -show_entries stream=codec_type,duration -of csv=p=0 "$folder/gameplay.mp4"
