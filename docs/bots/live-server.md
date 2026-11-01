# Playing on the live server

The game on itch.io, <https://nilvon9wo.itch.io/zaturnava>, is a page that plays on one game server:
<https://zaturnava.runasp.net/>. Your program can play on that same server, in the same games as the people
on the page: create a game and let them join it, join one of theirs, or watch. This page is everything you need
to connect, and what the server does and does not let anyone do.

## Where everything is

| | |
|---|---|
| The API's base address | `https://zaturnava.runasp.net/` (every route of [the HTTP API](http-api.md) is relative to it) |
| What the server hosts: rulesets, bots | `GET https://zaturnava.runasp.net/api/catalog` |
| Every route, as [OpenAPI](https://www.openapis.org/) | <https://zaturnava.runasp.net/openapi/v1.json>, to generate a client from |
| Every route, to try from the browser | <https://zaturnava.runasp.net/reference/v1> |
| The hosted MCP server, for an AI assistant | `https://zaturnava.runasp.net/mcp` ([MCP](mcp.md)) |
| The page, to watch your program play, or play against it | <https://nilvon9wo.itch.io/zaturnava> |

You need no account and no key. JSON over HTTPS is all there is.

## A first game, step by step

With `curl` (in a Unix shell; in PowerShell, call `curl.exe` and quote the JSON the PowerShell way):

```shell
# 1. A game: you at seat 0 (Black), the bot Sage at the other three.
curl -s https://zaturnava.runasp.net/api/games -H 'Content-Type: application/json' \
     -d '{"rulesetId":"chaturaji-2027","bots":[null,"sage","sage","sage"]}'
# -> {"gameId":"..."}

# 2. Your seat. Keep the token: it is shown once, and it is the only way to act for the seat.
curl -s -X POST https://zaturnava.runasp.net/api/games/$GAME/seats/0 \
     -H 'Content-Type: application/json' -d '{"nickname":"My bot"}'
# -> {"token":"..."}

# 3. The game as it stands: the board, whose decision it is, and what that seat may do.
curl -s https://zaturnava.runasp.net/api/games/$GAME

# 4. One of the legal actions, sent back as it came, with the seat's token.
curl -s https://zaturnava.runasp.net/api/games/$GAME/actions -H "Authorization: Bearer $TOKEN" \
     -H 'Content-Type: application/json' -d '{"kind":"roll","arguments":{"actor":"0"}}'
```

Then loop, as [Playing](http-api.md#playing) describes: read the game; if it is your seat's decision, send
one of `legalActions`; if a bot's, `POST api/games/{id}/advance` with the `version` you read; if a person's,
wait a second or two and read again. The game begins once every seat is taken, so leave a seat open
(`null`) only if you mean someone to join it: until they do, nothing is played.

Open the page meanwhile: your game is in the lobby, to watch, or, if you left a seat open, to join.

### In Python

Nothing beyond the standard library:

```python
import json, time, urllib.request

SERVER = "https://zaturnava.runasp.net/"

def call(method, route, body=None, token=None):
    request = urllib.request.Request(SERVER + route, method=method,
                                     data=None if body is None else json.dumps(body).encode())
    request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(request) as response:
        return json.load(response)

game = call("POST", "api/games", {"rulesetId": "chaturaji-2027", "bots": [None, "sage", "sage", "sage"]})
gid = game["gameId"]
token = call("POST", f"api/games/{gid}/seats/0", {"nickname": "My bot"})["token"]

while True:
    view = call("GET", f"api/games/{gid}")
    if view.get("outcome"):
        print(view["outcome"]["text"]); break
    seat = view["decidingSeat"]
    if seat == 0:
        call("POST", f"api/games/{gid}/actions", view["legalActions"][0], token)  # choose better than this
    elif view["seats"][seat]["controller"] == "bot":
        call("POST", f"api/games/{gid}/advance", {"version": view["version"]})
    else:
        time.sleep(2)
```

A refusal comes back as an HTTP error whose body is a [problem](http-api.md#refusals): `title` says which
refusal, `detail` why.

### In .NET

`Zaturnava.Client` (in this repository, `src/Apis/Zaturnava.Client`; reference the project) makes every call,
with the contracts as types:

```csharp
HttpGameClient games = new(new HttpClient { BaseAddress = new Uri("https://zaturnava.runasp.net/") });
GameCreatedDto created = await games.Create(
    new CreateGameRequest("chaturaji-2027", [null, "sage", "sage", "sage"]), cancellationToken);
SeatClaimedDto seat = await games.Claim(created.GameId, 0, null, "My bot", cancellationToken);
GameViewDto view = await games.Get(created.GameId, cancellationToken);
```

A refusal is a `GameRequestRefusedException`, whose `Refusal` says which.

### Through an AI assistant

Give it the hosted MCP server, and ask it to play: [MCP](mcp.md). For Claude Code:

```shell
claude mcp add --transport http zaturnava https://zaturnava.runasp.net/mcp
```

## What is protected, and how

The server is open by design: anyone may list the games, watch any of them, read its history once it is over,
create a game, and take an open seat. What it protects is who may act for a seat, and who may speak.

- **A seat is held by its token.** Claiming an open seat returns a 256-bit random token, once. The server keeps
  only its SHA-256 hash, so not even the server can show it again. Every action for the seat needs it, as
  `Authorization: Bearer {token}`. Anyone who has the token plays the seat, so keep it as you would a password:
  never in a URL, a log, or a message. Lose it and the seat is lost (the others may vote to stop waiting for it).
- **The rules are enforced by the server.** It accepts only legal actions, from the seat whose decision it is,
  and refuses anything else (`422 Illegal`, `403 Forbidden`). A client cannot cheat by sending what it likes.
- **People sign in on the page, never through your program.** A player's token comes only from signing in on
  the game's page, and only a seat taken signed in may speak at the table. Your program needs no such token,
  and should never ask anyone for theirs. It plays as a guest: it may read what is said at its table, but not
  speak. Its seat is shown as its nickname followed by "(guest)", so it can never pass for a person who signed
  in. A seat claimed for an AI (`"isAi": true`) is labelled as one, "(AI)" or "(AI guest)", and players may hide
  what AIs say.
- **Browsers may call the server only from the game's own page.** The server allows cross-origin requests from
  the itch.io page alone, so another web site cannot use a visitor's browser to play in their name. This does
  not limit programs: a script, a bot or a back-end calls the API directly. A web page of your own cannot call
  it from the browser; call it from your own server instead.
- **The server answers for its own name alone, and calls only itself.** A request naming another host is
  refused. The hosted MCP server calls the API at the server's configured address, never at one a request
  names.
- **No one may wear the server out for everyone else.** One address may make up to 600 requests a minute, and
  create up to 60 games an hour. Beyond that, requests are refused with `429` (`TooBusy`) until the minute (or
  hour) is over. A program that reads the game once a second, and advances bots only when it is their turn,
  never comes near: if yours does, slow down rather than retry at once. The limits may change; `429` is how you
  will know.
- **Nothing of yours is kept but what the game needs.** No account, no e-mail, no analytics. What is kept is in
  the [privacy notice](../privacy.md). Finished games' histories are public, to learn from, and name no one.

There is nothing to authenticate beyond that: no admin route, no route that deletes or changes another's game,
and no route that runs code. The server runs only the rulesets and bots it was built with.

## Being a good guest

- Create a game only to play it, and fill its seats: a game with an open seat waits for someone to take it.
- Poll gently: once a second is plenty for a game, every few seconds for the talk.
- The live server is a hobby's, on a free host: it may restart, be redeployed, or be down a while, and nothing
  about it is promised. To train, or to play thousands of games, use
  [self-play](self-play.md) or [a server of your own](README.md#running-a-server), not the live one.

## Found a weakness?

Please tell us privately, not in a public issue: see [SECURITY.md](../../SECURITY.md).
