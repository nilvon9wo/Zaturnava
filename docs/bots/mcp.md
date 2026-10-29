# Playing through MCP

An AI assistant that speaks the [Model Context Protocol](https://modelcontextprotocol.io) (Claude
Code, Claude Desktop, and many others) can play through `Zaturnava.Mcp`: a small MCP server, run on
your machine, that turns each of its tools into a call to a game server. Your assistant then plays as
a person would: it claims a seat, reads the board, and chooses among the legal actions.

Whatever model your assistant uses, it is yours: we never see its keys, and it costs us nothing.

## Setting it up

Build it once, from the repository's folder (you need the .NET SDK named in `global.json`):

```shell
dotnet build src/Apis/Zaturnava.Mcp
```

Then tell your MCP client how to start it. `ZATURNAVA_SERVER_URL` names the game server (by default
`http://localhost:5164/`, a [local one](README.md#running-a-server)). For Claude Desktop, in its
`claude_desktop_config.json`, or any client that takes the same shape:

```json
{
  "mcpServers": {
    "zaturnava": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Zaturnava/src/Apis/Zaturnava.Mcp", "--no-build"],
      "env": { "ZATURNAVA_SERVER_URL": "http://localhost:5164/" }
    }
  }
}
```

For Claude Code:

```shell
claude mcp add zaturnava --env ZATURNAVA_SERVER_URL=http://localhost:5164/ \
    -- dotnet run --project /path/to/Zaturnava/src/Apis/Zaturnava.Mcp --no-build
```

## The tools

| Tool | |
|---|---|
| `catalog` | The rulesets and the bots that can take a seat |
| `list_games` | The games, with their open seats |
| `create_game` | A game, each seat given a bot's id or left open |
| `claim_seat` | Claim an open seat; returns the token every action for it needs |
| `get_game` | The game as it stands: the board, the seats and scores, whose decision it is, and its legal actions |
| `submit_action` | Take one of the legal actions, with the seat's token |
| `advance_game` | Let the bot whose turn it is make one decision |
| `game_history` | The game as data: every action, the scores, and how it ended |

Each is one route of [the HTTP API](http-api.md), which explains what they take and return. A
refused request becomes the tool's error, with the server's reason, so the assistant can correct
itself.

## Asking it to play

Something like this is enough:

> Using the zaturnava tools, create a chaturaji-2027 game with seat 0 open and Sage on the other
> three seats, claim seat 0, and play it to the end. When it is another seat's decision, advance the
> game. Read the rules first: https://github.com/nilvon9wo/Zaturnava/blob/main/docs/rules.md

Open the page (<http://localhost:5165/>) to watch it play; the game is in the lobby.

A game takes many decisions, each a tool call or two, so it uses a fair amount of your model's
allowance. An assistant also plays far better with the rules in front of it: give it
[rules.md](../rules.md), and the history of a game or two to learn from ([game histories](game-histories.md)).
