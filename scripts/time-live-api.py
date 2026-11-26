"""Times the live server's requests the lobby makes, each a few times, to see which is slow.

Usage: py -3 scripts/time-live-api.py [base-url] [rounds]

Prints, for each request, its status, the bytes it returned and the seconds each round took. Only GETs that
anyone may make, no token sent.
"""

import sys
import time
import urllib.error
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "https://zaturnava.runasp.net/"
ROUNDS = int(sys.argv[2]) if len(sys.argv) > 2 else 3

PATHS = [
    "api/catalog",
    "api/games?game=chaturaji-2027",
    "api/games?game=grand-legions",
    "api/games?game=chaturaji-2027&over=true",
    "api/leaderboards/chaturaji-2027",
    "api/leaderboards/chaturaji-2027/bots",
    "api/leaderboards/grand-legions",
    "api/leaderboards/grand-legions/bots",
    "api/signin",
]


def timed(url: str) -> tuple[str, int, float]:
    request = urllib.request.Request(url, headers={"User-Agent": "zaturnava-timing"})
    start = time.perf_counter()
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            body = response.read()
            status = str(response.status)
    except urllib.error.HTTPError as error:
        body = error.read()
        status = str(error.code)
    except (urllib.error.URLError, TimeoutError) as error:
        return (f"error {error}", 0, time.perf_counter() - start)
    return (status, len(body), time.perf_counter() - start)


def main() -> None:
    for path in PATHS:
        results = [timed(BASE + path) for _ in range(ROUNDS)]
        seconds = " ".join(f"{result[2]:6.2f}" for result in results)
        print(f"{path:45} {results[-1][0]:>5} {results[-1][1]:>9} B  {seconds}")


if __name__ == "__main__":
    main()
