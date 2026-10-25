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
launch profiles, so Visual Studio's F5 does the same). One command builds both and runs them until
Ctrl+C; run it again to restart:

```shell
python3 scripts/play.py                                # then open http://localhost:5165
```

Don't rebuild them while they run: the page server indexes its files once, at startup, and a
rebuild beneath it (a test run from the same folder, say) leaves the page stuck on "Loading...".
Restart instead. By hand, that is:

```shell
dotnet build Zaturnava.Server && dotnet build Zaturnava.Web
dotnet run --project Zaturnava.Server --no-build       # http://localhost:5164/api/catalog
dotnet run --project Zaturnava.Web --no-build          # http://localhost:5165
```

Build first, then run both with `--no-build`: two builds at once collide on the shared, woven
assemblies.

- The page finds the server through `Zaturnava.Web/wwwroot/appsettings.json` (`Server:Url`).
- The server admits the page's origin through its page host; `appsettings.Development.json`
  chooses the `Listed` host, with `http://localhost:5165`.
- Games are kept in memory unless `Storage:Provider` says `Sqlite` or `SqlServer`, with
  `ConnectionStrings:Games`; for a file that survives restarts:

```shell
dotnet run --project Zaturnava.Server --no-build -- --Storage:Provider=Sqlite \
    "--ConnectionStrings:Games=Data Source=games.db"
```

Bots against bots, without a server:

```shell
dotnet run --project Zaturnava.BotRunner -- --games 10 --max-steps 200 --out games.jsonl
```

An AI client (Claude Code, Claude Desktop, any MCP client) plays through `Zaturnava.Mcp`, a stdio
MCP server; `ZATURNAVA_SERVER_URL` names the game server (default `http://localhost:5164/`):

```json
{
  "mcpServers": {
    "zaturnava": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Zaturnava/Zaturnava.Mcp", "--no-build"],
      "env": { "ZATURNAVA_SERVER_URL": "http://localhost:5164/" }
    }
  }
}
```

---

## Smart App Control (Windows)

Windows' Smart App Control blocks freshly built, unsigned DLLs by reputation, intermittently and
persistently per binary: a test host fails with `0x800711C7` ("An Application Control policy has
blocked this file"). Retrying doesn't reliably help.

A test module blocked before it starts is the worst case: `dotnet test` still prints "Test run
summary: Passed!" for the modules that did run. That is why `scripts/run-tests.py` exists: it
ends such a run with `Test run summary: INCONCLUSIVE - N test module(s) did not run` and exit
code 3.

Build and test from WSL instead, from a copy of the sources, so that the Linux restore doesn't
rewrite the `obj/` folders Visual Studio uses:

```shell
rsync -a --delete --exclude bin --exclude obj --exclude .vs --exclude .git --exclude TestResults \
    /mnt/e/path/to/Zaturnava/ ~/zaturnava/
cd ~/zaturnava && python3 scripts/run-tests.py --solution Zaturnava.slnx
```

Play from a second copy, so that a test run in the first never rebuilds the game beneath you
(WSL forwards the ports, so the page still opens in a Windows browser):

```shell
rsync -a --delete --exclude bin --exclude obj --exclude .vs --exclude .git --exclude TestResults \
    /mnt/e/path/to/Zaturnava/ ~/zaturnava-play/
cd ~/zaturnava-play && python3 scripts/play.py
```

The static gates (`dotnet format`, `check-line-layout.py`, `inspect-code.py`) run no test host
and work on Windows directly.

`rsync -a` keeps the Windows files' modification times. If you edit a file in the copy and then
sync over it, MSBuild can see nothing newer and skip rebuilding: delete the copy before a final
run.
