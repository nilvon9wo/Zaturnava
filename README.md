# Zaturnava

A 4-player chaturaji variant (originally conceived in Berlin), built as a configurable engine
rather than one hard-coded ruleset: the board, the teams, the seats, the pieces and the dice are
meant to change through configuration and small plugins, and every service and client around
the engine can be replaced on its own.

## Status

Playable end to end, with a partial ruleset: create a game in the browser, seat people and bots,
and move. The `classic` ruleset so far has the board, the pieces' moves and captures, scoring by
piece, and turns of one move or a pass; dice, throne control, promotion, boat triumph and game
end are still to come ([rules.md, implementation status](docs/rules.md#implementation-status)).

| Part | |
|---|---|
| `Zaturnava.Engine`, `.Topology.SquareGrid`, `.Variants.Classic` | The rules engine, a square-grid board, the base ruleset |
| `Zaturnava.Application`, `.Bots`, `.Persistence.*` | Games, seats and bots; storage in memory, SQLite or SQL Server |
| `Zaturnava.Server` | The game server: an HTTP API |
| `Zaturnava.Contracts`, `.Client` | The protocol, and a .NET client for it |
| `Zaturnava.Web`, `.Presentation` | The browser client (Blazor WebAssembly), meant for itch.io |
| `Zaturnava.Mcp` | An MCP server, so an AI can take a seat |
| `Zaturnava.BotRunner` | Bots playing bots, from the command line |

The game server's host is not decided yet; see
[architecture §8](docs/architecture.md#8-tech-stack--hosting).

## Documents

- [`docs/rules.md`](docs/rules.md) — the canonical rules for the base configuration (4 players,
  2 teams of 2 seated opposite each other, 8×8 board, traditional piece set), and every
  configuration toggle named so far. The source of truth for what the game *does*.
- [`docs/architecture.md`](docs/architecture.md) — how the code is shaped so those rules are one
  configuration of a general system: board topology, movement grammar, seats and teams, the
  pure-reducer engine, the action log, ports and adapters, the protocol, the clients, and
  hosting.
- [`docs/contribute/`](docs/contribute/README.md) — coding standards, running the game and the
  quality gates locally, tests and coverage, CI.

## Source material

- [`docs/origins/chaturaji.md`](docs/origins/chaturaji.md) and
  [`docs/origins/score-sheet.md`](docs/origins/score-sheet.md) — the original rules write-up and
  printable score sheet this project started from, converted from the Word and Excel files (still
  in the repository's history).

`docs/rules.md` is authoritative where it differs from the originals — several mechanics were
clarified or deliberately corrected through design discussion (see the *(clarified)* notes
throughout that file).
