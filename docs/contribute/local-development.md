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
- In development, `appsettings.Development.json` keeps games, players and their sign-ins in a SQLite file,
  `artifacts/local-play.db` (git-ignored), so that a restart forgets no one: a player signed in stays signed in
  until they sign out. Delete the file to start afresh. To keep them in memory instead, as the tests do:

```shell
dotnet run --project src/Hosts/Zaturnava.Server --no-build -- --Storage:Provider=InMemory
```

Signing in works on this machine as it does live, with each service the server is given a client for. A
service's OAuth app sends people back to one address only, so this machine needs apps of its own, beside the live
site's: for GitHub, an OAuth app (GitHub, Settings, Developer settings, OAuth Apps) with the homepage
`http://localhost:5165` and the callback `http://localhost:5164/signin-github`. Its id and a secret made for it go in
the server's user secrets, never in the repository (in WSL too, with `wsl -d Ubuntu --` before each, if the game runs
there):

```shell
dotnet user-secrets --project src/Hosts/Zaturnava.Server set "SignIn:Providers:GitHub:ClientId" "<its Client ID>"
dotnet user-secrets --project src/Hosts/Zaturnava.Server set "SignIn:Providers:GitHub:ClientSecret" "<its secret>"
```

Google and Discord are set up the same way, each under its own name (`SignIn:Providers:Google:...`,
`SignIn:Providers:Discord:...`): for Google, an OAuth client of the Web application type (Google Cloud console, APIs
and services, Credentials), with the redirect URI `http://localhost:5164/signin-google`; for Discord, an application
(Discord Developer Portal, OAuth2) with the redirect `http://localhost:5164/signin-discord`. itch.io's OAuth
applications (itch.io, Settings, API keys, OAuth applications) have no secret: only `SignIn:Providers:Itch:ClientId`
is set, and the redirect URI is `http://localhost:5164/signin-itch`. itch.io hands the token back to the browser, and
the server asks itch.io, with it, who the person is.

Without any, the page offers no sign-in, and everything else works as before.

The server brings the database's schema up to date as it starts. A change to `GameDbContext`'s model needs
a migration for each database, named for what it changes, which `dotnet ef` writes (from `dotnet-tools.json`;
the tests fail while one is missing):

```shell
dotnet tool restore
dotnet ef migrations add AddPlayers --project src/Persistence/Zaturnava.Persistence.EfCore.SqlServer --output-dir Migrations
dotnet ef migrations add AddPlayers --project src/Persistence/Zaturnava.Persistence.EfCore.Sqlite --output-dir Migrations
```

or, on a machine whose application control blocks the build's own assemblies, the same through WSL:
`wsl -d Ubuntu -- bash scripts/wsl-migrate.sh AddPlayers`.

Bots against bots, without a server, each game written as one line of JSON in the same shape the server
gives a game's history in (`GET api/games/{id}/history`):

```shell
dotnet run --project src/Bots/Zaturnava.BotRunner -- --games 10 --max-steps 200 --out games.jsonl
```

Sage's weights, tuned by Sage playing itself (here 2 rounds, each nudge tried over 40 games a side; how each
went is written to standard error). A nudge is kept only when it wins by more than twice the square root of
the games won or lost, so with few games a side little is kept. Copy what it writes into
`src/Bots/Zaturnava.Bots/Weights/`:

```shell
dotnet run --project src/Bots/Zaturnava.BotRunner -- --train 2 --games 40 --out chaturaji-2027.json
```

The bots' Elo ratings, from games between every pair of them (here 50 games a pair with each side
first; how each pair did is written to standard error). They go in `src/Bots/Zaturnava.Bots/Ratings/`,
and the leaderboard and each bot's profile show them:

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

To rate new or changed bots on the scale the live site uses, and ship them: `--hold-shipped` keeps the ratings
already shipped at the ruleset and fits only the other bots around them, as the server's bot-rating job does;
`--free` names shipped bots to measure again; `--ship` merges the ratings fitted into
`src/Bots/Zaturnava.Bots/Ratings/<ruleset>.json`, which the next deploy rates them by, and keeps the measurement,
each pair's results with it, in `docs/bots/rating-history.jsonl`, beside every one before it. Commit both. Local
games are the same software as the live site's, and far more of them, so they rate a bot better than the live job's
few games a pair:

```shell
wsl -d Ubuntu -- bash scripts/wsl-rate.sh --ruleset grand-legions-small --games 60 \
    --bots random,morpheus,phaethon,tantalus,midas --hold-shipped --free morpheus --ship
```

Train Athena the same way, in rounds, with whatever options `scripts/athena-rounds.py` takes, but the
runner ([self-play](../bots/self-play.md#learning)):

```shell
wsl -d Ubuntu -- bash scripts/wsl-athena.sh --rounds 6
```

To leave the machine training and rating the bots unattended, run both in turn, for every ruleset, cycle
after cycle, until stopped with Ctrl+C:

```shell
wsl -d Ubuntu -- bash scripts/wsl-overnight.sh
```

Each cycle runs, for each ruleset, rounds of Athena's learning and then an Elo tournament of every
bot. Athena's networks are kept in `artifacts/overnight/athena/<ruleset>/`, so each run carries on
from the last. Each tournament's ratings go to `artifacts/overnight/<started>/<ruleset>-<cycle>.json`.

Options:

| Option | Meaning | Default |
| --- | --- | --- |
| `--cycles N` | how many cycles to run | 0: until stopped |
| `--rulesets a,b` | which rulesets | `chaturaji-2027` and the three Grand Legions sizes |
| `--rounds N` | Athena's rounds per ruleset each cycle | 2 |
| `--games N` | games per round, and per pairing in the tournament | 100 |
| `--continue [DIR]` | go on in an earlier run's folder (the latest, if none is named), from the cycle it stopped in: a ruleset already rated is skipped, and one whose tournament had begun goes straight on with it, without Athena's rounds again | a new folder |
| `--ship` | hold the shipped bots at their ratings, and ship what each tournament measures of the others into `src/Bots/Zaturnava.Bots/Ratings/`, each measurement kept in `docs/bots/rating-history.jsonl`, for the next commit | nothing leaves `artifacts/` |

A failed stage is reported, and the run goes on. Each tournament keeps every pairing's result as it
ends, in `<ruleset>-<cycle>.json.pairs.json`, and prints it (`12/150 sage vs midas: ...`); with
`--continue`, a tournament stopped mid-way plays only the pairings it had not finished, and of those, only the
games not yet played: each game is kept as it ends, in `<ruleset>-<cycle>.json.games/`, so a stop loses at most
the games in play. Self-played
games keep the armies they were dealt, as the server's do, so they replay however the catalogue grows;
Athena leaves out any game that still does not replay (those kept before 2026-11-25), and says how many.

The Grand Legions tournaments are long: Sage takes about a minute a game there, start to finish, so each of its
pairings (twice `--games`) takes hours. Every five minutes, a line tells the pairings finished and each one playing
with its games so far (`... 14:05, 12/90 pairs done; playing sage vs midas 37/200, ...`); each pairing's own line
follows as it ends.

The static gates (`dotnet format`, `check-line-layout.py`, `inspect-code.py`) run no test host
and work on Windows directly.
