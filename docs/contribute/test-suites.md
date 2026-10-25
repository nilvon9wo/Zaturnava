# Test Suites

One test project per project, each measuring that project at 100% (see
[coverage-standards](coverage-standards.md)), plus one that checks how they depend on each other:

| Project | Proves |
|---|---|
| `Zaturnava.Engine.Test` | The movement grammar, move generation, positions, team graphs, action parsing. |
| `Zaturnava.Topology.SquareGrid.Test` | Stepping, direction families per facing, and layouts on rectangular grids. |
| `Zaturnava.Variants.Classic.Test` | The base ruleset: the setup, each piece's moves and captures, scoring, turn order. |
| `Zaturnava.Bots.Test` | `RandomBot` chooses among the legal actions, and honours cancellation. |
| `Zaturnava.Application.Test` | `GameService` — creating, claiming, acting, refusing — and the decision loop that lets bots reply. |
| `Zaturnava.Persistence.InMemory.Test` | The in-memory store, its optimistic concurrency included. |
| `Zaturnava.Persistence.EfCore.Test` | The EF Core store, against real SQLite, with test data from Xfty providers. |
| `Zaturnava.Hosting.Test` | The listed page host. |
| `Zaturnava.Hosting.Itch.Test` | itch.io's origins. |
| `Zaturnava.Contracts.Test` | Routes, refusal statuses and their exception, and that every DTO survives the trip through JSON. |
| `Zaturnava.Client.Test` | `HttpGameClient`: each request, and each refusal turned back into its exception. |
| `Zaturnava.Server.Test` | The whole HTTP API, through `WebApplicationFactory` and the real `HttpGameClient`; raw HTTP for what a client can't send (CORS preflights, forged tokens, malformed bodies); storage and page host selection; that the contract's action argument names are the engine's. |
| `Zaturnava.Presentation.Test` | Board frames, which cells are clickable, and how clicks become actions. |
| `Zaturnava.Web.Test` | The Blazor components with bUnit: the lobby, the table, the board. |
| `Zaturnava.Mcp.Test` | The MCP tools, over in-memory pipes with the SDK's own client, against a stub game server. |
| `Zaturnava.BotRunner.Test` | Option parsing, self-play, the JSON lines it writes, exit codes. |
| `Zaturnava.Architecture.Test` | Every project uses only what its row of the `MayUse` table allows, and every project in `Zaturnava.slnx` has a row. Not measured. |

```shell
python3 scripts/run-tests.py --solution Zaturnava.slnx                  # everything
dotnet test --project Zaturnava.Engine.Test                             # one project, with its coverage gate
```

`scripts/verify-doc-links.py` checks that every relative link in `README.md` and `docs/`, and
every `#anchor`, resolves.
