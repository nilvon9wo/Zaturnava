# Zaturnava — Architecture (v0.2 draft)

How the code is shaped so that the base ruleset in [rules.md](rules.md) is one
*configuration* of a more general system rather than the system itself, and so
that every service, client, board and piece set around it can be replaced
without touching the rest.

Sections marked **(design)** describe where the engine is going; everything
else describes the code as it stands. [rules.md](rules.md#implementation-status)
lists which rules are implemented so far.

## 1. Design Goals

- The rules engine does not know or care what drives it: a browser, a phone,
  a console, a bot, an AI over MCP — or what transport carries the requests.
- Changing board shape or size, army count, team topology, piece set, dice
  behavior, promotion rules or win conditions means writing or selecting a
  configuration and small pluggable policies, not editing move legality or
  turn sequencing.
- The engine is deterministic and side-effect-free: the same starting state
  and the same actions always produce the same result. That is what makes it
  testable in isolation, replayable, and transport-agnostic.
- Every dependency outside the engine — storage, hosting, notification,
  clients — sits behind an interface (a *port*) with one or more replaceable
  implementations (*adapters*), chosen by configuration in one place.
- Async first, sync compatible: "submit a move and come back later" works
  today; a live layer can be added later without changing the engine or the
  stored data.
- A seat is played by a human, a bot or an AI, and the engine cannot tell
  which.

## 2. Layered Abstraction Model

### 2.1 Board topology

The board is a set of cells and a way to move between them, never an assumed
8×8 grid. `ICellNavigating` (Engine) gives the cells (`CellId`, an opaque
number), the neighbour one step in a `Direction`, the concrete directions of a
named `DirectionSet` (orthogonal, diagonal, knight, forward, forward
diagonals) for a seat facing a given way, and a `CellLayout` for drawing.

`SquareGridTopology(width, height)` (Topology.SquareGrid) is the only
implementation so far. A hex topology would be another implementation of the
same interface: pieces and move generation only ever step along directions,
never through coordinates. Only the layout — where a client draws a cell —
mentions geometry, and it travels to clients as data (`CellDto` with a
`Shape`).

### 2.2 Piece catalog and movement grammar

A piece kind is data: a `PieceKind` holding a list of `MovementRule`s, each
"travel along these directions between *m* and *n* steps, blocked or leaping,
onto an empty cell, a capture, or either". `MovementRule.Step`, `Slide` and
`Leap`, narrowed by `MoveOnly` and `CaptureOnly`, express all five base pieces
(Games.Chaturaji2027 `Chaturaji2027Pieces`); `MoveGenerator` (Engine) turns them into
moves on any topology. A new piece for a variant is a new entry, not new
engine code.

The grammar reaches further, for the catalogue of Grand Legions:

- **Leapers** of any shape: `DirectionSets.Leaper(3, 2)` is the Zebra's eight
  leaps; `Slide` along one is a rider (the Knightrider).
- **Lame leaps** (`Lame`): blocked by a piece on any cell the leap passes,
  which the topology names (`ICellNavigating.WaypointsOf`): straight first,
  then diagonally, as the Xiangqi Horse and Elephants go.
- **Hops** (`Hop`): over the first piece on the line, to the cells beyond, up
  to and taking the next piece (the Cannons), or only just beyond (the
  Grasshopper); `Shunning` names kinds it may neither hop nor take (a Janggi
  Cannon's Cannons).
- **Sideways** and **any** (orthogonal and diagonal) direction sets.

A piece's *identity* (its display name) is kept apart from its *behaviour*:
`PieceKindId` is a key, and the ruleset's `NameOf` gives the label, so
"Elephant" against a chess-styled "Rook" is a naming choice, not a code
change.

### 2.3 Seats, players, and teams

- A **seat** (`Seat`, `SeatId`) is one army's structural slot, independent of
  who plays it. So far it carries the direction its army calls forward; its
  pieces' starting cells are the ruleset's setup, and its throne will join it
  with throne control.
- A **controller** is bound to each seat when a game is created
  (`SeatController`: an open seat waiting for a person, a person holding the
  seat's token, or a named bot). Anything that decides a seat implements
  `IDeciding`; the engine only ever asks the rules for the legal actions of the
  deciding seat.
- **Teams** are a `TeamGraph` over the seats, not an assumption of two pairs
  facing each other: adjacent teammates, 3-against-1 or free-for-all are other
  graphs. Team membership is fixed for the lifetime of a game.

### 2.4 Controlled Armies — the unifying primitive

**(design)** Three rules in rules.md §6–7 are expressions of one concept:

```
ControlledArmies(decisionMaker, moment) =
    { decisionMaker's own army }
  ∪ { army of any seat whose throne decisionMaker's own Raja currently occupies }
  ∪ { army of any ally who currently has no Raja on the board at all }
```

The third term is independent of throne occupation — it fires purely off the
ally's Raja-less state (rules.md §6.1(b)), so it can apply even when
`decisionMaker`'s Raja isn't sitting on anyone's throne.

- **Expanded-pool movement** (rules.md §6.1): on a player's own turn, the
  legal move pool is `ControlledArmies(thatPlayer, now)`.
- **Doubles-escape scope** (rules.md §7) reuses the same function: an
  escaping piece's *category* is checked against every army in
  `ControlledArmies(decisionMaker, now)`.
- **Regency delegation and forced shared turns** (rules.md §6.2, §6.3) are the
  one deliberate exception, keyed on *who is deciding*: when someone other
  than the seat's own player makes the decision, the pool is just that seat's
  native army. The occupied player's *own* first move within a §6.3 forced
  shared turn is not the exception — it draws normally from
  `ControlledArmies(occupiedPlayer, now)`.

One function with one exception path is simpler to get right, and to keep
right as mechanics are layered on, than three independent rule systems.

### 2.5 Turn / phase state machine

**(design)** Several rules need multi-step turns with sub-decisions
(roll or move, which die to act on first, whether to decline a move, a
mid-turn hand-off per §6.2). A turn becomes an explicit sequence of phases —
e.g. `ChooseModeAndRoll → ResolveDieResult(1) → ResolveDieResult(2) →
TurnComplete` — with a delegation phase that can hand the remaining phases to
another decision-maker. The engine already models this much of it:
`GameState.DecidingSeat` names whose decision the game is waiting for, which
need not be the seat whose turn it is.

### 2.6 Rule modules

**(design)** Mechanics that are exceptions to plain movement — throne
control, boat triumph, promotion and ransom, dice, scoring bonuses — become
discrete modules hooked around the core move application (before a move,
after a move, at the start of a turn). The core applies a legal move and asks
each module whether anything else happens; it never itself knows what a Boat
Triumph is.

## 3. Engine as a Pure Reducer

A ruleset is an `IRulesApplying`:

```
CreateInitialState()            -> GameState
LegalActions(GameState)         -> GameAction[]        (empty once the game is over)
Apply(GameState, GameAction)    -> Transition(GameState, GameEvent[])
Parse(ActionDescriptor)         -> GameAction
```

- No I/O, no database, no clock. Chance is a port of its own: a ruleset
  that needs dice says so (`GameState.AwaitedDice`), and whatever runs the
  game rolls them through `IDiceRolling` and applies the result as a
  `DiceAction`. A replay or a test supplies the rolls that actually
  happened; the server rolls `RandomDice`. A player can ask for a roll
  (`roll`) but never choose its values.
- `GameState` is an immutable record; every action produces a new state and
  the domain events (`PieceMoved`, `PieceCaptured`, `TurnPassed`, …) that
  everything else can be driven from.
- An action crosses process boundaries as an `ActionDescriptor` — a kind and
  named string arguments (`actor`, `from`, `to`) — which the ruleset parses
  back. Clients never need the engine's types.
- An illegal action is refused (`IllegalActionException`); a malformed one
  likewise (`MalformedActionException`). Nothing is ever partly applied.

## 4. Persistence Model

A game is stored as its ruleset, its seat controllers and the **list of every
action taken** (`GameRecord`), never as a state snapshot: the state is
replayed from the initial state. That gives replay and audit for free, and
fits asynchronous play — there is no session to keep.

Each save increments `GameRecord.Version`, and `IGamePersisting.Replace` only
overwrites the version one less, so two concurrent changes cannot both win
(`ConcurrencyConflictException`, answered as 409).

Adapters, chosen by the server's `Storage:Provider` setting:

| Provider | Project | Use |
|---|---|---|
| `InMemory` (default) | Persistence.InMemory | Development, tests; lost on restart. |
| `Sqlite` | Persistence.EfCore.Sqlite | A single file; `ConnectionStrings:Games`. |
| `SqlServer` | Persistence.EfCore.SqlServer | A hosted database; `ConnectionStrings:Games`. |

Each database project holds its provider's EF Core migrations, and the server applies them as it starts
(`GameSchema.MigrateGamesAsync`), so a database is never migrated by hand. A database whose tables were made
before there were migrations is first marked as having had the first one, which makes the same tables.

A periodic snapshot beside the action list is a later optimization, if replay
ever costs enough to matter.

**Players** (Proposed) are kept beside the games, through `IPlayerPersisting`: each player has an id, a
name and the logins they sign in with (a provider and the key that provider knows them by). Signing in
(`PlayerService.SignIn`) finds the player by their login, or makes one, and issues a session token, of which
only a hash is kept, as with seat tokens.

## 5. Projects and Dependencies

Ports and adapters: the engine at the centre depends on nothing; the
application layer depends only on the engine and declares the ports it needs;
adapters implement them; the server is the one composition root that knows
every concrete choice. The client side shares nothing with the server but the
contracts.

On disk, each project sits under `src/`, in a folder for its area, and its tests under `tests/`, in
the same folder: `Engine` (the engine and its topologies), `Games` (one folder per game, such as
`Games/Chaturaji2027` and `Games/GrandLegions`), `Bots`, `Application`, `Apis` (contracts,
client, MCP, presentation), `Hosts` (server, page, hosting adapters) and `Persistence`.
`Zaturnava.Architecture.Test` sits at the top of `tests/`.

| Project | Role | May use |
|---|---|---|
| Engine | Pure rules primitives: cells, pieces, moves, state, `IRulesApplying`, `IDeciding` | — |
| Topology.SquareGrid | `ICellNavigating` for rectangular grids | Engine |
| Games.Chaturaji2027 | Chaturaji 2027, the base game (`Chaturaji2027Rules`) | Engine, Topology.SquareGrid |
| Games.GrandLegions | Grand Legions (the grand game), so far: its catalogue of pieces as data (`GrandLegionsPieces`), each piece's class, tradition, value, movement, restrictions and promotions; the board as a game is set (`GrandBoard`: kingdoms, rivers); teams (`Alliances`); and the dice that name a class (`ClassDice`) | Engine |
| Bots | Bot players, each describing itself (`BotProfile`: name, description, whether it uses AI): `RandomBot`, at random but for agreeing to end the game only while its team leads, as every bot does; `MidasBot`, greedy for the best immediate score, trading prisoners only at a profit, counting worth and points; `SolomonBot`, as greedy on the board, giving allies' prisoners back and trading evenly by worth alone; `ArgusBot`, weighing each action against the worst reply (what the ruleset's `AttacksOf` says the enemy could take, and how the exchange would end), and each trade by worth less the exposure it adds; `SageBot`, looking through the rest of its own turn with the dice averaged over every way they could fall (`Foresight`), weighing where it leads by weighted features any ruleset lets it count (`Appraisal`, `SageWeights`), and each trade by the position it leaves. Each bot weighs a ransom its own way (`ITradeJudging`: `ByWorth`, `ByExposure`, `ByPosition`). With an enemy, every bot but Random and Sage trades only what its team can afford: no leader given back but for as much in return; no more asked than three times what is given, and no trade that leaves its team weaker on the board; and no bot makes again an offer declined lately (within three rounds, with the worth on the board changed by less than a tenth, and no offer made since by the seat that declined) | Engine |
| Application | Use cases (`GameService`, `PlayerService`), the decision loop, catalogs of rulesets and bots; ports `IGamePersisting`, `IPlayerPersisting`, `IGameNotifying` | Engine |
| Persistence.InMemory | `IGamePersisting` and `IPlayerPersisting` in memory | Application, Engine |
| Persistence.EfCore | `IGamePersisting` and `IPlayerPersisting` over EF Core, naming no database; bringing the schema up to date (`GameSchema`) | Application, Engine |
| Persistence.EfCore.Sqlite | The context on SQLite, and its migrations | Persistence.EfCore |
| Persistence.EfCore.SqlServer | The context on SQL Server, and its migrations | Persistence.EfCore |
| Hosting | Ports for the services around the game (`IPageHosting`), and `ListedPageHost` | — |
| Hosting.Itch | `IPageHosting` for a page published on itch.io | Hosting |
| Contracts | The wire protocol: DTOs, routes, refusals, `IGameServing` | — |
| Client | `IGameServing` over HTTP (`HttpGameClient`) | Contracts |
| Presentation | Client-side board logic: frame, clickable actions, selection | Contracts |
| Web | Blazor WebAssembly client | Client, Contracts, Presentation |
| Mcp | MCP server (stdio) letting an AI play through any server | Client, Contracts |
| BotRunner | Command-line self-play: bots against bots (one's own among them, from an assembly), each game's history as a JSON line; tuning Sage, and training Athena's network | Application, Bots, Engine, Games.Chaturaji2027 |
| Server | ASP.NET Core host; the composition root | Application, Bots, Client, Contracts, Engine, Hosting, Hosting.Itch, Mcp, Persistence.*, Topology.SquareGrid, Games.Chaturaji2027 |

**Vendors stay in their adapters.** A third-party service — a page host, a
game host, a database provider — is named only in its own adapter project and
in the composition root that selects it by a provider name from
configuration. Nothing else in the solution, its configuration keys included,
knows which service is in use.

The table is enforced: `Zaturnava.Architecture.Test` reads every project
reference and fails on any dependency not listed, and on any project in
`Zaturnava.slnx` the table does not know.

What each replaceable part is swapped through:

| To replace | Implement | Register in |
|---|---|---|
| Board shape | `ICellNavigating` | the ruleset |
| Piece set | `PieceKind` entries | the ruleset |
| Ruleset / variant | `IRulesApplying` | Server `ZaturnavaServices` (and BotRunner) |
| Bot | `IDeciding` | the bot catalog in the same places |
| Storage | `IGamePersisting`, `IPlayerPersisting` | Server `GameStorage`, chosen by `Storage:Provider` |
| Where the page is served from | `IPageHosting` | Server `PageHosting`, chosen by `PageHosting:Provider` |
| Notification / push | `IGameNotifying` (now `SilentNotifier`: clients poll) | Server `ZaturnavaServices` |
| Server, for a client | `IGameServing` | the client's composition (`Server:Url`, `ZATURNAVA_SERVER_URL`) |
| Client | anything speaking §6 | — |

## 6. Protocol

HTTP and JSON (`JsonSerializerOptions.Web`), routes relative to the server's
base address (`GameRoutes`):

| Request | Route | Answer |
|---|---|---|
| Rulesets and bots on offer: each ruleset with its seats' names and teams, each bot with its name, description, whether it uses AI, and its rating at each ruleset | `GET api/catalog` | `CatalogDto` |
| Games | `GET api/games` | `GameSummaryDto[]` |
| Create a game | `POST api/games` with `CreateGameRequest` (a ruleset, per seat a bot id or null for a person, and, for a game played again, the game it continues) | 201, `GameCreatedDto` |
| A game | `GET api/games/{id}` | `GameViewDto`: cells (with names and thrones), pieces, seats (with scores, bot names and prisoners), the dice and what each allows, the latest events, whose decision and for which seat, the legal actions (labelled where the kind alone cannot say), how the game ended, whether the deciding seat may offer a ransom, the offer waiting on its answer, the time left on a timed decision, and the cell of a Pawn whose promotion is due |
| A game's history | `GET api/games/{id}/history` | `GameHistoryDto`: each seat (its name, team, who played it and its score), every action in order, the dice among them, and how it ended (the winners, the kind of ending as the ruleset's code, and why), or null while it goes on: the shape the BotRunner writes self-play games in |
| Every finished game's history | `GET api/history` | `GameHistoryDto[]` |
| Let a bot or a timer decide | `POST api/games/{id}/advance` with `AdvanceRequest` (the version the caller saw) | `GameViewDto` after one bot decision, or the timer's decision once a timed one has run out of time; unchanged if the version is stale or neither is due |
| Claim an open seat | `POST api/games/{id}/seats/{seat}` | `SeatClaimedDto` with the seat's token |
| Act | `POST api/games/{id}/actions` with `ActionDto`, `Authorization: Bearer {token}` | `GameViewDto` after the action and any bot replies |
| What was said at the table | `GET api/games/{id}/chat?after={n}`, the token optional | `ChatMessageDto[]` after message `n`: what was said to the table, and to the token holder's team |
| Say something | `POST api/games/{id}/chat` with `SayRequest` (`table` or `team`, and the words), `Authorization: Bearer {token}` | `ChatMessageDto`, numbered within its table |

- **Talk** is kept per table (`ChatTables`): a table is known by its first
  game, and a game played again after another (`CreateGameRequest.Continuing`)
  sits at the same one. Once no player has read or said anything there for
  `Chat:LeftAfterSeconds` (default 120), the players have left, and the
  `ChatSweeper`, looking every `Chat:SweepSeconds` (default 30), lets the
  table's talk go. Which game sits at which table, and when it was last heard
  from, are kept in memory: after a restart, every table counts as heard from
  when the server began.
- **Seat tokens** are the only credential. One is issued when a seat is
  claimed, shown once, and stored only as a hash; no accounts, no personal
  data.
- **Bots** decide one step per `advance`, so that a client can show their
  turns at a pace people can follow (`Table:BotPaceSeconds`, default 0.9).
  Dice a bot awaits are always rolled and do not count as a step. The server
  can still reply with up to `Games:MaximumBotSteps` bot steps inside the
  request that made it a bot's turn; the default is 0.
- **The log** in a view is the latest 40 events, each told in the ruleset's
  own names and the topology's cell names ("Black rolled 2 (Horse) and 5
  (Pawn or Raja).", "Yellow moved their Pawn from b5 to c5, without
  rolling.").
- **Timed decisions**: a ruleset names the decisions that may take only so
  long, and what is decided once time is up (`IRulesApplying.TimerOf`); the
  host says how long, by name (`Games:Timers:{name}`, in seconds;
  `Games:Timers:Default`, 30, for any not named). The time runs from when
  the server first saw the game at its version, in memory, so a restart
  gives a waiting decision its full time again. The server never acts on its
  own: a client showing a countdown advances the game once it runs out.
- **Offers** of ransom are not among the legal actions, which would have to
  list every combination of prisoners. The view says when the deciding seat
  may offer, and a client builds the action with `OfferDto.ActionOf`; the
  rules refuse an offer that may not be made, as they refuse any illegal
  action.
- **Acting for another seat**: while one seat moves for another (an enemy
  Raja on a throne shares that seat's turn), the view's `DecidingSeat` is the
  seat that decides, holding the token that may act, and `ActingFor` is the
  seat whose turn it is.
- **Refusals** are RFC 9457 problem details, and each `Refusal` has one
  status: Invalid 400, Forbidden 403, NotFound 404, Conflict 409, Illegal 422.
  `HttpGameClient` turns them back into `GameRequestRefusedException`, so a
  client sees the same refusal however it is connected.
- **CORS** admits the origins of the page host (`IPageHosting`) chosen by
  `PageHosting:Provider`: `Itch`, or `Listed` with its origins under
  `PageHosting:Origins`, each under a name of its own so that another settings
  file adds an origin rather than replacing one.
- **Change notification**: none yet; the Web client polls every
  `Table:PollSeconds` (default 2). A push channel would be an `IGameNotifying`
  adapter plus a client subscription, with no change to the engine or the
  stored data.

## 7. Clients

- **Web** — Blazor WebAssembly, a static site of about 16 MB, meant to be
  uploaded to itch.io as an HTML game. It renders the board as SVG from the
  cell layouts it is sent, so a different topology needs no client change.
  `wwwroot/appsettings.json` names the server (`Server:Url`): the live one;
  `appsettings.Development.json`, the one on this machine. It is uploaded to itch.io by hand,
  through `.github/workflows/deploy-itch.yml` (butler, with an itch.io API key in the repository's secrets).
- **MCP** — a stdio MCP server whose tools (`catalog`, `list_games`,
  `create_game`, `claim_seat`, `get_game`, `game_history`, `submit_action`, `advance_game`) call a game server
  through `IGameServing`; an AI plays exactly as a person would, by claiming a
  seat and acting with its token. The Server hosts the same tools over HTTP at `mcp` (stateless), calling
  its own HTTP API at the address the request came to.
- **BotRunner** — runs bots against bots in-process, for testing rulesets and
  bots, and writes each game's history as a JSON line (`--ruleset`, `--bots`,
  `--games`, `--max-steps`, `--out`); `--bot-assembly` seats bots of one's own
  beside ours ([self-play](bots/self-play.md)). With `--train`, it tunes Sage's weights for the
  ruleset instead, by Sage playing itself, and writes them as JSON for
  `Zaturnava.Bots/Weights/{ruleset}.json`, which Sage plays by. Each game it
  writes says who won, from which `scripts/rate-bots.py` fits the bots' Elo
  ratings, kept in `Zaturnava.Bots/Ratings/{ruleset}.json`.
- Android, iPhone, Windows or command-line clients would reuse Contracts and
  Client (or speak the protocol directly); Presentation holds board logic that
  is not Blazor-specific.

## 8. Tech Stack & Hosting

| Concern | Choice |
|---|---|
| Language, runtime | C#, .NET 10 |
| Engine, application | Plain class libraries; parameter preconditions by Stipulate; `ConfigureAwait(false)` woven by ConfigureAwait.Fody |
| Server | ASP.NET Core minimal API |
| Storage | EF Core (SQLite, SQL Server) or in memory |
| Web client | Blazor WebAssembly |
| AI access | ModelContextProtocol SDK |
| Tests | xUnit v3 on Microsoft Testing Platform, bUnit, Xfty test data; 100% line and branch coverage per project |

### Hosting — status: **leading option, not decided**

Constraints (2026-10-24): free, **with no credit card on file anywhere**, so
that neither popularity nor abuse can produce a bill; every provider
replaceable.

- **Front door: itch.io** (agreed), hosting the Web client as an HTML game.
- **Game server: MonsterASP.NET free tier** is the leading candidate: no
  card, EU datacenter, ASP.NET Core and SQL Server, 256 MB RAM, 1 GB database,
  WebSockets. Its limits: the site stops when idle and starts on the next
  request; free HTTPS is a Let's Encrypt certificate, turned on in its control panel, which renews it; a
  free site unused for six
  months is archived; no backups. Its traffic limits are still to be
  confirmed with their support.
- **Fallback**: a relay on Cloudflare's free plan behind the same protocol.

The game server is at `zaturnava.runasp.net`, sent there by hand through
`.github/workflows/deploy-monsterasp.yml` (Web Deploy, with the site's publishing profile in
the repository's secrets); another host would be another workflow, not a change to the server.
With a `MONSTERASP_SQLSERVER_CONNECTION_STRING` secret (the site's SQL Server database, from its control panel), the
workflow writes it into the published `appsettings.Production.json`, with `Storage:Provider` set to
`SqlServer`, so that games outlast a restart; without one, games are kept in memory.

Nothing in the code depends on the host: the server reads its storage, page
host and limits from configuration, and the clients read the server's address
from theirs. The game server's host, once chosen, gets an adapter of its own
for whatever it needs that is specific to it.

The table-top platforms that could have hosted the game outright were ruled
out: Game Courier's four-handed chaturanga does not enforce rules, Board Game
Arena licenses only published games, and Zillions of Games' piece model cannot
express controlled armies (§2.4). For discoverability, being *listed* —
chessvariants.com and BoardGameGeek pages linking to the itch.io page — reaches
the same audience.

## 9. Configuration Surface (cross-reference)

Per-game **rule** toggles (dice profile, promotion pool, end-game safety nets,
scoring) are listed in [rules.md §12](rules.md#12-configuration-toggles). The
**structural** configuration is chosen by the ruleset:

| Axis | Base configuration | Generalizes to |
|---|---|---|
| Board topology | 8×8 square grid | Any size; hex grid; non-grid topologies |
| Seat count / layout | 4 seats, pinwheel corners | Any arrangement the topology supports |
| Team graph | 2 teams of 2, opposite seats | Adjacent teammates, 3v1, free-for-all |
| Piece catalog | Raja, Elephant, Horse, Boat, Pawn | Any piece the movement grammar expresses |
| Seat controller | Chosen per seat at creation | Person, bot or AI, in any mix |

## 10. Open Questions / Deferred

- Hosting waits until the game is playable on one machine against simple
  bots. Then, the game server's host (§8): confirm MonsterASP.NET's traffic limits and
  archiving rules, and its site address for the Web client's production
  settings.
- A push channel to replace polling (§6).
- The turn phase machine (§2.5), controlled armies
  (§2.4) and rule modules (§2.6), as the remaining rules are implemented.
- Exact `gameEnd.safetyNet.*` parameters (rules.md §13) — now that bots exist,
  bots advance one step at a time, but the rules-level
  defaults are still open.
- An SDK for variants: once Chaturaji 2027 works, boards, piece sets,
  starting positions, rulesets and bots become things others implement. Their
  extension points should then be gathered into a documented SDK, with
  examples checked by the build (as Stipulate's documentation examples are).
- Fluid team membership mid-game — out of scope, a possible future variant.
- Whether a throne should follow a relocated Raja starting square, for
  configurations that vary where the Raja starts (rules.md §2) — only matters
  once such a variance exists.
- Piece naming and the Boat's identity (rules.md §13) — cosmetic only; the
  movement grammar (§2.2) is unaffected either way.
