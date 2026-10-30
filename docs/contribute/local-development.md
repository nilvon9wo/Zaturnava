# Local Development

A standard .NET solution, `Zaturnava.slnx`. Prerequisites: the .NET 10 SDK, and Python 3 for
the gate scripts.

---

## The loop

```shell
dotnet build Zaturnava.slnx                                         # .editorconfig rules enforced
dotnet format Zaturnava.slnx --verify-no-changes --severity info
python3 scripts/check-line-layout.py                                # 120 characters, wrapped ')'
dotnet tool restore && python3 scripts/inspect-code.py              # ReSharper inspections
python3 scripts/run-tests.py --solution Zaturnava.slnx --no-build
python3 scripts/verify-doc-links.py
python3 scripts/verify-gates.py                                     # every gate still fires
```

These are the gates CI runs (see [ci](ci.md)). `dotnet format`, `check-line-layout.py` and
`inspect-code.py` are the ones most easily forgotten: the build passes without them, CI won't.
`inspect-code.py` needs a prior build.

Every test run enforces 100% coverage, which a filtered run can't reach; pass
`-p:ZaturnavaCoverage=false` for those (see [coverage-standards](coverage-standards.md)). The
report, a `coverage.cobertura.*.xml` under the test project's `bin/.../TestResults/`, shows which
line or branch is missing.

---

## Running the game

The server and the web client run side by side, the server on port 5164, the page on 5165 (their
launch profiles). In Visual Studio (2022 17.11 or later), the **Play** launch profile, shared in
`Zaturnava.slnLaunch`, starts both: choose it beside the start button. One command does the same
from a shell, building both and running them until Ctrl+C; run it again to restart:

```shell
python3 scripts/play.py                                # then open http://localhost:5165
```

Don't rebuild them while they run: the page server indexes its files once, at startup, and a
rebuild beneath it (a test run from the same folder, say) leaves the page stuck on "Loading...".
Restart instead. By hand, that is:

```shell
dotnet build src/Hosts/Zaturnava.Server && dotnet build src/Hosts/Zaturnava.Web
dotnet run --project src/Hosts/Zaturnava.Server --no-build # http://localhost:5164/api/catalog
dotnet run --project src/Hosts/Zaturnava.Web --no-build    # http://localhost:5165
```

Build first, then run both with `--no-build`: two builds at once collide on the shared, woven
assemblies.

- The page finds the server through `src/Hosts/Zaturnava.Web/wwwroot/appsettings.json` (`Server:Url`).
- The server admits the page's origin through its page host; `appsettings.Development.json`
  chooses the `Listed` host, with `http://localhost:5165`.
- Games are kept in memory unless `Storage:Provider` says `Sqlite` or `SqlServer`, with
  `ConnectionStrings:Games`; for a file that survives restarts:

```shell
dotnet run --project src/Hosts/Zaturnava.Server --no-build -- --Storage:Provider=Sqlite \
    "--ConnectionStrings:Games=Data Source=games.db"
```

Bots against bots, without a server, each game written as one line of JSON in the same shape the server
gives a game's history in (`GET api/games/{id}/history`):

```shell
dotnet run --project src/Bots/Zaturnava.BotRunner -- --games 10 --max-steps 200 --out games.jsonl
```

Sage's weights, tuned by Sage playing itself (here 2 rounds, each nudge tried over 40 games a side; how each
went is written to standard error). Copy what it writes into `src/Bots/Zaturnava.Bots/Weights/`:

```shell
dotnet run --project src/Bots/Zaturnava.BotRunner -- --train 2 --games 40 --out chaturaji-2027.json
```

The bots' Elo ratings, from games between every pair of them (here 50 games a pair with each side
first; how each pair did is written to standard error). They go in `src/Bots/Zaturnava.Bots/Ratings/`,
and "The bots" shows them:

```shell
python3 scripts/rate-bots.py path/to/Net.NowhereAtAll.Zaturnava.BotRunner --games 50     --out src/Bots/Zaturnava.Bots/Ratings/chaturaji-2027.json
```

A development server also offers games set up part-way, to start from: see
[scenarios/README.md](../../scenarios/README.md).

An AI client (Claude Code, Claude Desktop, any MCP client) plays through `Zaturnava.Mcp`, a stdio
MCP server: see [playing through MCP](../bots/mcp.md). Bots of your own play ours in self-play with
`--bot-assembly`: see [self-play](../bots/self-play.md).

---

## Smart App Control (Windows)

Windows' Smart App Control blocks freshly built, unsigned DLLs by reputation, intermittently and
persistently per binary: a test host fails with `0x800711C7` ("An Application Control policy has
blocked this file"). Retrying doesn't reliably help.

A test module blocked before it starts is the worst case: `dotnet test` still prints "Test run
summary: Passed!" for the modules that did run. That is why `scripts/run-tests.py` exists: it
ends such a run with `Test run summary: INCONCLUSIVE - N test module(s) did not run` and exit
code 3.

Build and test from WSL instead, in the checkout itself, from its root. The build goes to
`artifacts/wsl/` (git-ignored), apart from the `bin/` and `obj/` folders Visual Studio uses, so the
Linux restore never rewrites them:

```shell
wsl -d Ubuntu -- bash scripts/wsl-test.sh                       # every test project
wsl -d Ubuntu -- bash scripts/wsl-test.sh Zaturnava.Bots.Test   # or just these
```

Play the same way; it builds to `artifacts/wsl-play/`, so that a test run never rebuilds the game
beneath you (WSL forwards the ports, so the page still opens in a Windows browser):

```shell
wsl -d Ubuntu -- bash scripts/wsl-play.sh
```

Rate the bots the same way, with a Release build of the BotRunner in `artifacts/wsl-release/`; it
takes whatever options `scripts/rate-bots.py` does, but the runner:

```shell
wsl -d Ubuntu -- bash scripts/wsl-rate.sh --bots random,sage,athena --games 50
```

The static gates (`dotnet format`, `check-line-layout.py`, `inspect-code.py`) run no test host
and work on Windows directly.
