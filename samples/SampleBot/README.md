# A sample bot

A complete bot, in about three hundred lines of C#, that plays Zaturnava's games on any of its servers, the live
one included (the one the [itch.io page](https://nilvon9wo.itch.io/zaturnava) plays on). It is a starting point:
copy it, and replace its mind with yours.

It stands alone. It references none of Zaturnava's code and imports none of its build settings: it talks to the
server over [the HTTP API](../../docs/bots/http-api.md), as a program in any language may. It could live in a
repository of its own, and one day will.

Its mind is mechanical, no AI at all, so running it costs nothing: it takes the most valuable enemy piece it can,
else moves at random, and never gives anything away. That is about as strong as the server's Midas. Its point is
the loop around the mind, which any bot needs.

## Running it

With the [.NET 10 SDK](https://dotnet.microsoft.com/download):

```shell
cd samples/SampleBot
# A game of its own, against the server's bots (Sage, by default, in every other seat):
dotnet run -- --server https://zaturnava.runasp.net/
# Against others of them, in turn:
dotnet run -- --server https://zaturnava.runasp.net/ --opponents random,midas,solomon
# A seat left open for it in someone's game, such as one set up on the page for "Your own AI":
dotnet run -- --server https://zaturnava.runasp.net/ --join {the game's id} --seat {the seat, from 0}
```

| Option | | Default |
|---|---|---|
| `--server` | The server's address | (required) |
| `--ruleset` | The game, for a game of its own (`GET api/catalog` lists them) | `chaturaji-2027` |
| `--opponents` | The server's bots for the other seats, by id, comma-separated, taken in turn | `sage` |
| `--join`, `--seat` | Play this seat of this game, instead of a game of its own | |
| `--nickname` | The name the seat shows, followed by "(guest)" | `Sample bot` |
| `--pause-ms` | How long to wait between requests: be a good guest | `250` |

It plays as a guest. To play as a signed-in player's AI instead, sign in on the page, make an AI token under
**Profile**, and put it in the environment as `ZATURNAVA_AI_TOKEN` (never on the command line, where others may
see it). The seat then shows the player's name, marked as an AI's.

From this repository, under WSL: `wsl -d Ubuntu -- bash scripts/wsl-sample-bot.sh --server ...`.

## How it is built

| File | |
|---|---|
| `Program.cs` | The command line: create a game or join one, claim the seat, play it |
| `GameApi.cs` | The HTTP API's routes, as the bot calls them |
| `Contracts.cs` | The parts of the server's JSON the bot reads |
| `Player.cs` | The loop: read the game; decide if it is the bot's turn; let a server bot move if it is theirs; else wait |
| `IChoosing.cs` | The mind: which legal action to take. **Replace this** |
| `Greedy.cs` | The mechanical mind it comes with |

## Putting an AI in it

Write another `IChoosing` and pass it to the `Player` in `Program.cs`. `IChoosing.cs` says how, in short:

- **A language model**: give it the game and the legal actions, numbered, and ask for a number. Check the answer,
  and fall back on `Greedy` if it is not one of them. Keep the model's key in the environment.
- **A model of your own**, as the server's Athena is: a small neural network that judges positions. Learn from
  the server's [game histories](../../docs/bots/game-histories.md), and train against the server's bots offline
  with [self-play](../../docs/bots/self-play.md).

An AI assistant that speaks MCP (Claude Code, say) needs no program at all: give it the server's
[MCP tools](../../docs/bots/mcp.md), which call these same routes.
