# Zaturnava

A 4-player chaturaji variant (originally conceived in Berlin), built as a configurable engine
rather than one hard-coded ruleset: the board, the teams, the seats, the pieces and the dice are
meant to change through configuration and small plugins, and every service and client around
the engine can be replaced on its own.

## Play it

You need the .NET SDK named in `global.json`. Then, from the repository's folder:

```shell
python3 scripts/play.py      # or: py -3 scripts/play.py on Windows
```

and open <http://localhost:5165>. It builds and starts the game server (port 5164) and the page
(port 5165), and stops both on Ctrl+C. In Visual Studio, pick the **Play** launch profile beside
the start button (it starts both projects) and press it. Don't rebuild while the game runs; restart
it instead. More in [running the game](docs/contribute/local-development.md#running-the-game).

## Status

Playable end to end, online: <https://nilvon9wo.itch.io/zaturnava> is the page, and
<https://zaturnava.runasp.net/> the game server it plays on. Create a game, seat people and bots, sign in (or
play as a guest), talk at the table, and play. The `chaturaji-2027` ruleset has nearly all of the rules: the
board, the pieces' moves and captures, the roll-off, dice, scoring with the throne bonuses and the empire,
command of other armies through thrones, regency, escape, promotion, ransom and the end of the game; the boat
triumph and the per-game toggles are still to come
([rules.md, implementation status](docs/rules.md#implementation-status)).

| Part | |
|---|---|
| `Zaturnava.Engine`, `.Topology.SquareGrid`, `.Games.Chaturaji2027` | The rules engine, a square-grid board, Chaturaji 2027 |
| `Zaturnava.Games.GrandLegions` | Grand Legions, the grand game: so far its catalogue of pieces, its board and its dice |
| `Zaturnava.Application`, `.Bots`, `.Persistence.*` | Games, seats and bots; storage in memory, SQLite or SQL Server |
| `Zaturnava.Server` | The game server: an HTTP API |
| `Zaturnava.Contracts`, `.Client` | The protocol, and a .NET client for it |
| `Zaturnava.Web`, `.Presentation` | The browser client (Blazor WebAssembly), meant for itch.io |
| `Zaturnava.Mcp` | An MCP server, so an AI can take a seat |
| `Zaturnava.BotRunner` | Bots playing bots, yours among them, from the command line |

Where it is hosted, and how it is deployed, is in
[architecture §8](docs/architecture.md#8-tech-stack--hosting). A program of yours may play on the live server
too: see [the live server](docs/bots/live-server.md).

## Documents

- [`docs/rules.md`](docs/rules.md) — the canonical rules for the base configuration (4 players,
  2 teams of 2 seated opposite each other, 8×8 board, traditional piece set), and every
  configuration toggle named so far. The source of truth for what the game *does*.
- [`docs/architecture.md`](docs/architecture.md) — how the code is shaped so those rules are one
  configuration of a general system: board topology, movement grammar, seats and teams, the
  pure-reducer engine, the action log, ports and adapters, the protocol, the clients, and
  hosting.
- [`docs/bots/`](docs/bots/README.md) — bring your own AI: play it through MCP or the HTTP API,
  download the games played, and train it against our bots in self-play.
- [`docs/contribute/`](docs/contribute/README.md) — coding standards, running the game and the
  quality gates locally, tests and coverage, CI.
- [`docs/privacy.md`](docs/privacy.md) — what is kept about the people who play, and how to have
  it deleted.
- [`SECURITY.md`](SECURITY.md) — how to report a weakness privately, and what the server protects.

## Source material

- [`docs/origins/chaturaji.md`](docs/origins/chaturaji.md) and
  [`docs/origins/score-sheet.md`](docs/origins/score-sheet.md) — the original rules write-up and
  printable score sheet this project started from, converted from the Word and Excel files (still
  in the repository's history).

`docs/rules.md` is authoritative where it differs from the originals — several mechanics were
clarified or deliberately corrected through design discussion (see the *(clarified)* notes
throughout that file).
