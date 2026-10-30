#!/usr/bin/env python3
"""Pictures of the page for its itch.io listing, kept in --folder (default artifacts/media, git-ignored).

It drives the page in a headless Chromium (Playwright), against a server and page already running on this
machine (scripts/wsl-play.sh), and sets games up through the server's HTTP API: bots on every seat, moved on
so far before the page watches them. It makes:

- 01-lobby.png .. 05-late.png: screenshots, --width by --height;
- cover.png: 630 by 500, a game in the middle, taken at twice that and shrunk (ffmpeg);
- gameplay.mp4: a game of bots watched from its start for --seconds, with its sounds (Playwright records the
  picture alone, as webm; the page's sounds are recorded in it, and ffmpeg puts the two together as mp4).

Playwright and ffmpeg are not the repository's: under WSL, from the repository's root,

    sudo apt install -y python3-venv ffmpeg
    python3 -m venv artifacts/tools/venv
    artifacts/tools/venv/bin/pip install playwright
    sudo artifacts/tools/venv/bin/playwright install-deps chromium
    artifacts/tools/venv/bin/playwright install chromium

Usage: artifacts/tools/venv/bin/python scripts/capture.py [--server http://localhost:5164/]
           [--page http://localhost:5165/] [--folder artifacts/media] [--width 1280] [--height 800]
           [--seconds 75]
"""

import argparse
import base64
import glob
import json
import os
import shutil
import subprocess
import time
import urllib.request

from playwright.sync_api import sync_playwright


# Playwright's video has no sound. The page makes its sounds with Web Audio, each to its AudioContext's destination:
# here that destination is a node passing them on to the speakers and to a recorder as well, whose recording is put
# under the video. When it began (the wall clock, in ms) tells where in the video it goes.
SOUND_TAP = """
(() => {
    const Native = window.AudioContext;
    if (!Native) return;
    window.__sound = { chunks: [], startedAt: null, recorder: null };
    window.AudioContext = class extends Native {
        constructor(...args) {
            super(...args);
            const mix = this.createGain();
            const tap = this.createMediaStreamDestination();
            mix.connect(super.destination);
            mix.connect(tap);
            Object.defineProperty(this, "destination", { get: () => mix });
            const recorder = new MediaRecorder(tap.stream, { mimeType: "audio/webm" });
            recorder.ondataavailable = event => window.__sound.chunks.push(event.data);
            recorder.start(1000);
            window.__sound.recorder = recorder;
            window.__sound.startedAt = Date.now();
        }
    };
})();
"""

# The recording, stopped, as a data: URL, and when it began; null if the page made no sound.
SOUND_TAKEN = """
async () => {
    const sound = window.__sound;
    if (!sound || !sound.recorder) return null;
    await new Promise(stopped => { sound.recorder.onstop = stopped; sound.recorder.stop(); });
    const blob = new Blob(sound.chunks, { type: "audio/webm" });
    const url = await new Promise(read => {
        const reader = new FileReader();
        reader.onload = () => read(reader.result);
        reader.readAsDataURL(blob);
    });
    return { startedAt: sound.startedAt, url };
}
"""


def call(server, method, route, body=None):
    """The server's JSON reply to one request."""
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(
        server + route, data=data, method=method, headers={"Content-Type": "application/json"}
    )
    with urllib.request.urlopen(request) as reply:
        return json.loads(reply.read() or b"null")


def bots_of(catalog):
    """Four bots for the seats: those that cost nothing, the strongest last, one seat each while there are enough."""
    free = [bot["id"] for bot in catalog["bots"] if not bot.get("isPaid") and not bot.get("usesAi")]
    return [free[-1 - (seat % len(free))] for seat in range(4)]


def started(server, catalog, decisions):
    """A game of bots, moved on by so many decisions, and still going (if it ended, another, moved on by half as
    many: a game over cannot be watched); its id."""
    body = {"rulesetId": catalog["rulesets"][0]["id"], "bots": bots_of(catalog)}
    game = call(server, "POST", "api/games", body)["gameId"]
    view = call(server, "GET", f"api/games/{game}")
    for _ in range(decisions):
        if view.get("outcome") is not None:
            return started(server, catalog, decisions // 2)
        view = call(server, "POST", f"api/games/{game}/advance", {"version": view["version"]})
    return game if view.get("outcome") is None else started(server, catalog, decisions // 2)


def watching(page, url, game):
    """The page, watching the game from the lobby."""
    page.goto(url)
    page.locator(f"li:has(span.game:text-is('{game[:8]}')) button.watch").click()
    page.locator("svg.board").wait_for()
    page.wait_for_timeout(1500)


def shrunk(source, target, width, height):
    subprocess.run(
        ["ffmpeg", "-y", "-loglevel", "error", "-i", source, "-vf", f"scale={width}:{height}", target], check=True
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--server", default="http://localhost:5164/")
    parser.add_argument("--page", default="http://localhost:5165/")
    parser.add_argument("--folder", default="artifacts/media")
    parser.add_argument("--width", type=int, default=1280)
    parser.add_argument("--height", type=int, default=800)
    parser.add_argument("--seconds", type=int, default=75)
    options = parser.parse_args()
    os.makedirs(options.folder, exist_ok=True)

    def at(name):
        return os.path.join(options.folder, name)

    catalog = call(options.server, "GET", "api/catalog")
    size = {"width": options.width, "height": options.height}
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(args=["--autoplay-policy=no-user-gesture-required"])

        page = browser.new_page(viewport=size)
        page.goto(options.page)
        page.locator("button.create").wait_for()
        page.screenshot(path=at("01-lobby.png"))
        print("01-lobby.png")

        middle = started(options.server, catalog, 60)
        watching(page, options.page, middle)
        page.screenshot(path=at("02-middle.png"))
        print("02-middle.png")

        page.locator("button.theme").first.click()
        page.locator("button.pieces").first.click()
        page.wait_for_timeout(800)
        page.screenshot(path=at("03-light-chess.png"))
        print("03-light-chess.png")
        page.locator("button.theme").first.click()
        page.locator("button.pieces").first.click()

        page.locator("button.how-to-play").first.click()
        page.wait_for_timeout(800)
        page.screenshot(path=at("04-how-to-play.png"))
        print("04-how-to-play.png")

        page.reload()
        late = started(options.server, catalog, 400)
        watching(page, options.page, late)
        page.screenshot(path=at("05-late.png"))
        print("05-late.png")
        page.close()

        cover = browser.new_page(viewport={"width": 1260, "height": 1000})
        watching(cover, options.page, started(options.server, catalog, 90))
        cover.screenshot(path=at("cover-large.png"))
        cover.close()
        shrunk(at("cover-large.png"), at("cover.png"), 630, 500)
        print("cover.png")

        recorded = at("recording")
        shutil.rmtree(recorded, ignore_errors=True)
        context = browser.new_context(viewport=size, record_video_dir=recorded, record_video_size=size)
        context.add_init_script(SOUND_TAP)
        video = context.new_page()
        video_began = time.time()
        watching(video, options.page, started(options.server, catalog, 0))
        video.wait_for_timeout(options.seconds * 1000)
        sound = video.evaluate(SOUND_TAKEN)
        context.close()
        browser.close()

    webm = glob.glob(os.path.join(recorded, "*.webm"))[0]
    audio = []
    if sound is not None:
        with open(at("sound.webm"), "wb") as file:
            file.write(base64.b64decode(sound["url"].split(",", 1)[1]))
        offset = max(0.0, sound["startedAt"] / 1000 - video_began)
        audio = ["-itsoffset", f"{offset:.3f}", "-i", at("sound.webm"), "-map", "0:v", "-map", "1:a", "-c:a", "aac"]
    else:
        print("the page made no sound: the video is silent")
    subprocess.run(
        ["ffmpeg", "-y", "-loglevel", "error", "-i", webm, *audio]
        + ["-c:v", "libx264", "-pix_fmt", "yuv420p", "-movflags", "+faststart", at("gameplay.mp4")],
        check=True,
    )
    print("gameplay.mp4")


if __name__ == "__main__":
    main()
