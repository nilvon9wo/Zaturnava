# Self-play: your bot against ours

To train a bot, or to find out how good it is, it must play many games, far more than anyone would sit
through. The BotRunner plays them offline, as fast as your machine allows, with no server: your bot
and ours, on whichever seats you like, each game written down as its [history](game-histories.md).

Your bot is .NET: a class implementing `IBotPlaying`, in an assembly of your own. (A bot in another
language plays through [the HTTP API](http-api.md) instead, against a server; a learner in another
language can still learn from the histories self-play writes.)

## A bot

Clone the repository, and make a class library that references the engine:

```xml
<ItemGroup>
  <ProjectReference Include="/path/to/Zaturnava/src/Engine/Zaturnava.Engine/Zaturnava.Engine.csproj" />
</ItemGroup>
```

A bot is told everything about its decision: the ruleset (which can apply any action to any state,
so a bot can look ahead), the state of the game, its seat, and the actions it may take. It returns one
of them:

```csharp
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

public sealed class MyBot(Random random) : IBotPlaying
{
    public BotProfile Profile { get; } = new("mine", "Mine", "What it does, for people.", UsesAi: false);

    public Task<GameAction> Decide(DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[random.Next(request.LegalActions.Count)]);
}
```

`request.Rules.Apply(request.State, action).State` is the state an action leads to;
`request.State.Scores`, `request.State.Position` and the rest describe it. A bot that would play
well, and not just legally, can start from ours, in `src/Bots/Zaturnava.Bots`: Sage looks ahead
through the dice and weighs each position (`Foresight`, `Appraisal`).

The BotRunner makes every public bot in your assembly: with a `Random` if its constructor takes one,
else with none. Its `Profile.Id` is how you seat it, and must not be one of ours.

## Playing

Build the BotRunner once, then seat your bot by its id, beside ours:

```shell
dotnet build src/Bots/Zaturnava.BotRunner
dotnet run --project src/Bots/Zaturnava.BotRunner --no-build -- \
    --bot-assembly /path/to/MyBots/bin/Release/net10.0/MyBots.dll \
    --bots mine,sage,mine,sage --games 200 --out games.jsonl
```

Seats 0 and 2 are a team, as are 1 and 3, so that is your bot's team against Sage's. Each line of
`games.jsonl` is one game's history: who won, how, and by how much. Play as many with the teams
swapped (`--bots sage,mine,sage,mine`), as moving first is worth something.

| Option | |
|---|---|
| `--bots` | A bot id per seat, by commas: `random`, `midas`, `solomon`, `argus`, `sage`, `tactician`, `athena`, `pythia`, or yours |
| `--games` | How many games (default 100); they play in parallel |
| `--max-steps` | The most decisions in a game before it is cut off, unfinished (default 1000) |
| `--out` | The file to write (default the console) |
| `--bot-assembly` | Your assembly |

## Rating

`scripts/rate-bots.py` plays every pair of bots against each other, both ways round, and fits Elo
ratings to the results, anchored at Random's 1000, the same ratings "The bots" shows in the game:

```shell
python3 scripts/rate-bots.py path/to/Net.NowhereAtAll.Zaturnava.BotRunner \
    --bot-assembly /path/to/MyBots.dll --bots random,sage,mine --games 50
```

(The runner is the BotRunner's built executable, in its `bin` folder.) Each pair's line says how long its games
took, too.

Or, through the test runner alone: write `artifacts/arena/request.json`, such as
`{"bots": ["sage", "tactician"], "ruleset": "chaturaji-2027", "games": 10, "maximumSteps": 2000}`, and run the
BotRunner's tests (`wsl -d Ubuntu -- bash scripts/wsl-test.sh --release Zaturnava.BotRunner.Test`). Its arena
(`ArenaRun`) plays every pair of the bots that many games with each team moving first, and writes how each pair
fared, and how long it took, to `artifacts/arena/results.json`, a pair at a time. With no request, it is skipped,
as on CI.

### The server's rating job

The server rates its own bots too, in the background, with no command to run (*Stated 2026-11-12*: every bot,
a good mix of rulesets, each result kept as it comes). While `BotRating:IsOn` (on in `appsettings.json`), its
`BotRater` plays one game after another, pausing `BotRating:PauseSeconds` (default 10) between them so that
people's games go first. *(Proposed)*:

- Each game is the pairing of two bots, at a ruleset, played least so far, so every pair at every ruleset is
  played about as often; the two take the even and the odd seats, and change sides each game. A pairing of a bot
  not yet rated at its ruleset with one that is (Random, the anchor, always counting as rated) goes first, as
  only those games rate it.
- A game that reaches `BotRating:MaximumSteps` decisions (default 2000) goes to the side with more points, as
  the script counts it.
- Each game is added to its pairing's wins, losses and draws (the `BotMatchups` table) as soon as it ends, so a
  run stopped at any time still counts.
- A bot not rated at a ruleset, once it has played each rated bot there `BotRating:MinimumGames` times
  (default 2), is rated by those games against the rated bots' ratings, held as they are (*Proposed
  2026-11-13*: one scale, the one the server shipped with; a fit of every bot to a few games each draws every
  rating toward Random's). This is checked after each game, and from the games kept whenever the server starts.
  A bot already rated keeps its rating; Random, if not rated, is rated 1000.
- A game that fails is let go, and the next goes on.

## Fairy-Stockfish, as a bot

[Fairy-Stockfish](https://github.com/fairy-stockfish/Fairy-Stockfish), a strong engine for many chess
variants, can be asked for its move (*Stated 2026-11-12*: "Let's try it"). It is an experiment, for the
BotRunner and the tests only, not the server: the engine is GPL-3, so it is never kept in the repository nor
shipped, but run as a program of its own, spoken to over UCI. `src/Bots/Zaturnava.Bots.FairyStockfish` is a
bot assembly, its bot's id `fairy-stockfish`. *(Proposed)*:

- It looks for the engine where `FAIRY_STOCKFISH_PATH` says, else for a file named `fairy-stockfish*` in
  `artifacts/fairy-stockfish` (gitignored) or any folder in it, from the nearest folder up that has one. The
  large-board build (`largeboard`) is needed for boards over 8 by 8; a Windows build runs from WSL too.
- Fairy-Stockfish plays two sides, not four seats: the seat's team is White, to move, and every other team
  Black. Each piece is defined in Betza notation (`BetzaNotation`) facing as its seat faces, so a Pawn facing
  east is a kind of its own; a side that loses every leader has lost. Each position's variant is written to
  `variants` beside the engine, named for a hash of its definition, and loaded once.
- Of the moves the dice allow, it plays the one the engine names (searching only those), thinking 200 ms.
- Athena decides everything else: drafting, ransom, ending a game, a single move or none; and any game the
  engine cannot see: a board over 12 by 10 or not a rectangle, more than 25 kinds of piece, or a piece the
  notation cannot say (a slide that must go some distance first, a hop that may land some way beyond, a piece
  that may not hop over some kinds, an uneven leap only some ways round).

With the engine there, the tests `LiveEngineRun` and `LiveBearingRun` play it; without, as on CI, they are
skipped. To rate it in the arena, add `"botAssembly"` to the request, the path of its built assembly (such as
`artifacts/wsl/bin/Zaturnava.Bots.FairyStockfish/release/Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.dll`).

First results (2026-11-12, Chaturaji 2027, 10 games each way): it beat Random 20 to 0, but lost to Athena 7 to
13. Too few games to be sure, but no sign yet that it is the stronger: it sees neither the dice nor the points,
only the two sides' pieces and the leaders.

## Learning

Self-play makes data as fast as you can use it: play your bot against itself and ours, learn from the
histories (replaying them to see each position: [game histories](game-histories.md#seeing-every-position)),
and play again. That is how Sage's weights were tuned (`--train`), and how Athena, our learned bot, was
trained:

```shell
dotnet run --project src/Bots/Zaturnava.BotRunner -c Release -- \
    --games 1000 --bots sage,sage,sage,sage --max-steps 3000 --out sage.jsonl
dotnet run --project src/Bots/Zaturnava.BotRunner -c Release -- \
    --learn sage.jsonl,more.jsonl --epochs 10 --hidden 32 --out chaturaji-2027.json
```

`--learn` replays every finished game of the ruleset in the files, takes every position from every seat,
and trains a small network to predict how the game went for that seat's team: won, drawn or lost, and by
how many points. With `--lambda` below 1, each position is taught partly what the network judges of the
positions after it (temporal differences, as TD-Gammon learned). The network starts out judging as Sage
does, so that what it learns corrects Sage; or from a network learned before (`--from`). It writes the
network as JSON; put it in `src/Bots/Zaturnava.Bots/Networks/`, named for the ruleset, and Athena plays by
it, or give it to `--network` to play her by it without building her again. The positions are given to the
network as `PositionEncoder` sums them up, which your own learner may use too. What did and did not work is
in the [backlog](../backlog.md#bots).

| Option | |
|---|---|
| `--learn` | The files of game histories to learn from, comma-separated |
| `--from` | A network's file to go on from, instead of starting from Sage's judgement |
| `--epochs` | How many times to go through every position (default 8) |
| `--hidden` | How many hidden units a new network has (default 32) |
| `--lambda` | How much each position is taught how the game went, rather than what follows it, from 0 to 1 (default 1) |
| `--rate` | How big a step the network takes towards each position, from 0 to 1, shared out over how big its numbers are (default 0.1) |
| `--network` | A network's file for Athena to play by, when playing games |

`scripts/athena-rounds.py` runs all of it in rounds, each learning from every game so far, then playing
Athena against Sage, 200 games with each team moving first, and keeping those games for the next round. Each
round's network, games and result go to `artifacts/athena/` (`rounds.jsonl`; the best network so far is
`best.json`). From Windows, through WSL ([local development](../contribute/local-development.md)):

```shell
wsl -d Ubuntu -- bash scripts/wsl-athena.sh --rounds 6 --seed sage.jsonl,more.jsonl
```
