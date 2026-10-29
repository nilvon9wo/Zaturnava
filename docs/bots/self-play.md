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
| `--bots` | One bot id per seat, comma-separated: `random`, `midas`, `solomon`, `argus`, `sage`, or yours |
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

(The runner is the BotRunner's built executable, in its `bin` folder.)

## Learning

Self-play makes data as fast as you can use it: play your bot against itself and ours, learn from the
histories (replaying them to see each position: [game histories](game-histories.md#seeing-every-position)),
and play again. That is how Sage's weights were tuned (`--train`), and how the
[learned bot we plan](../backlog.md#bots) will be trained.
