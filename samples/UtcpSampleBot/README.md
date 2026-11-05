# A sample bot, over UTCP

A complete bot, in about three hundred and fifty lines of C#, that plays Zaturnava's games on any of its servers,
the live one included (the one the [itch.io page](https://nilvon9wo.itch.io/zaturnava) plays on). It is a starting
point: copy it, and replace its mind with yours.

It reads the server's [UTCP manual](../../docs/bots/utcp.md), at `{server}/utcp`, and calls each tool as the manual
says: the [Universal Tool Calling Protocol](https://www.utcp.io) tells a client how to call a tool's own address
itself, with no server of tools in between. Its twins play the same way, [`HttpApiSampleBot`](../HttpApiSampleBot/README.md)
over plain HTTP, and [`McpSampleBot`](../McpSampleBot/README.md) through the server's MCP tools.

It stands alone. It references none of Zaturnava's code and imports none of its build settings, nor any package:
UTCP has no official C# library yet, so it carries a small client of its own (`UtcpClient.cs`), enough for tools
called over HTTP. It could live in a repository of its own, and one day will.

Its mind is mechanical, no AI at all, so running it costs nothing: it takes the most valuable enemy piece it can,
else moves at random, and never gives anything away. That is about as strong as the server's Midas. Its point is
the loop around the mind, which any bot needs.

## Running it

With the [.NET 10 SDK](https://dotnet.microsoft.com/download):

```shell
cd samples/UtcpSampleBot
# A game of its own, as Black, the server's Sage in every other seat:
dotnet run -- --server https://zaturnava.runasp.net/
# As Yellow, with Random as its ally (Red) and Midas and Sage as its opponents (Black, then Green):
dotnet run -- --server https://zaturnava.runasp.net/ --seat Yellow --allies random --opponents midas,sage
# A seat left open for it in someone's game, such as one set up on the page for "Your own AI":
dotnet run -- --server https://zaturnava.runasp.net/ --join {the game's id} --seat Red
```

Seats go by the names the game gives them, as on the board: in Chaturaji 2027, Black and Green are one team,
Yellow and Red the other. Before a game of its own begins, the bot says who sits where:

```text
Black: midas (opponent)
Yellow: this bot
Green: sage (opponent)
Red: random (ally)
```

| Option | | Default |
|---|---|---|
| `--server` | The server's address; its manual is at `utcp` under it | (required) |
| `--ruleset` | The game, for a game of its own (the `catalog` tool lists them, with their seats and teams) | `chaturaji-2027` |
| `--seat` | The bot's own seat, by name (any case) | the first |
| `--allies` | The server's bots for its allies' seats, by id, comma-separated, in seat order; in a game of everyone for themselves, none | `sage` |
| `--opponents` | The server's bots for its opponents' seats, likewise | `sage` |
| `--join` | Play `--seat` of this game, instead of a game of its own | |
| `--nickname` | The name the seat shows, followed by "(AI guest)" | `UTCP sample bot` |
| `--pause-ms` | How long to wait between steps: be a good guest | `250` |

A list shorter than the seats it fills is taken again from its start.

It plays as an AI guest. To play as a signed-in player's AI instead, sign in on the page, make an AI token under
**Profile**, and put it in the environment as `ZATURNAVA_AI_TOKEN` (never on the command line, where others may
see it). The seat then shows the player's name, marked as an AI's.

From this repository, under WSL: `wsl -d Ubuntu -- bash scripts/wsl-utcp-sample-bot.sh --server ...`.

## How it is built

| File | |
|---|---|
| `Program.cs` | The command line: read the manual, create a game or join one, claim the seat, play it |
| `UtcpClient.cs` | A small UTCP client: reads a manual, and calls a tool as its HTTP call template says |
| `GameTools.cs` | The game's tools, as the bot calls them |
| `Contracts.cs` | The parts of the tools' JSON the bot reads |
| `Player.cs` | The loop: read the game; decide if it is the bot's turn; let a server bot move if it is theirs; else wait |
| `IChoosing.cs` | The mind: which legal action to take. **Replace this** |
| `Greedy.cs` | The mechanical mind it comes with |

## Putting an AI in it

Write another `IChoosing` and pass it to the `Player` in `Program.cs`. `IChoosing.cs` says how, in short:

- **A language model**: give it the game and the legal actions, numbered, and ask for a number. Check the answer,
  and fall back on `Greedy` if it is not one of them. Keep the model's key in the environment. An agent that speaks
  UTCP itself can be given the manual, and play by the tools directly.
- **A model of your own**, as the server's Athena is: a small neural network that judges positions. Learn from
  the server's [game histories](../../docs/bots/game-histories.md) (the `game_history` tool), and train against the
  server's bots offline with [self-play](../../docs/bots/self-play.md).
