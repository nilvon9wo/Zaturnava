# The HTTP API

Everything a client can do, in any language: JSON over HTTP, no account, no key. The browser page
and the MCP server use these same routes. The server's address below is a local one,
`http://localhost:5164/` ([running a server](README.md#running-a-server)); the routes are relative to
it. Names in JSON are camelCase. The server's own address (and `api`) answers with where to go from there,
and `openapi/v1.json` describes every route as [OpenAPI](https://www.openapis.org/), for tools that generate
clients. `reference/v1` shows the same as a page, where each route can be tried from the browser. The live
server is at `https://zaturnava.runasp.net/`.

## Routes

| | Request | Reply |
|---|---|---|
| What the server hosts | `GET api/catalog` | The rulesets (with their seats' names) and the bots, by id |
| The games | `GET api/games` | Each game: its id, its open seats, who plays each seat (`open`, `person` or a bot's id), whether it is over |
| Create a game | `POST api/games` | `201`, `{"gameId": "..."}` |
| Claim a seat | `POST api/games/{id}/seats/{seat}` | `{"token": "..."}` |
| A game | `GET api/games/{id}` | The game as it stands ([the view](#the-view)) |
| Act | `POST api/games/{id}/actions`, with the seat's token | The game after it |
| Let a bot (or a timer) decide | `POST api/games/{id}/advance` | The game after one decision |
| A game's history | `GET api/games/{id}/history` | [Game histories](game-histories.md) |
| Every finished game's history | `GET api/history` | A list of them |
| What was said at the table | `GET api/games/{id}/chat?after={n}`, with a seat's token or none | [The talk](#talk-at-the-table) since message `n` |
| Say something | `POST api/games/{id}/chat`, with the seat's token | The message, numbered |

A game is created with its ruleset and, for each seat in order, the id of the bot that plays it, or
`null` to leave the seat open:

```json
{ "rulesetId": "chaturaji-2027", "bots": [null, "sage", "sage", "sage"] }
```

Claiming an open seat returns its **token**, once: the server keeps only its hash, and nothing else
can act for the seat. Send it with every action, as `Authorization: Bearer {token}`. A seat is the
whole of what a token holds: there are no accounts.

## Playing

The server never acts by itself; a client moves the game along. A game waits on its **deciding
seat** (`decidingSeat` in the view; `null` once the game is over):

1. If it is your seat, choose one of `legalActions` and post it back, as it came.
2. If a bot plays it, post `advance` with the `version` you saw, `{"version": 12}`, and the bot makes
   one decision. If the game has moved on meanwhile (someone else advanced it), nothing happens; read
   it again.
3. If a person plays it, wait, and read the game again in a second or two. There is no push channel
   yet: the page polls too.

A loop, then:

```text
claim a seat -> token
loop:
    view = GET api/games/{id}
    if view.outcome: done
    if view.decidingSeat == my seat: POST api/games/{id}/actions (one of view.legalActions), with token
    else if that seat is a bot's (view.seats[decidingSeat].controller == "bot"):
        POST api/games/{id}/advance {"version": view.version}
    else: wait a moment
```

With `curl`:

```shell
curl -s localhost:5164/api/games -H 'Content-Type: application/json' \
     -d '{"rulesetId":"chaturaji-2027","bots":[null,"sage","sage","sage"]}'
curl -s -X POST localhost:5164/api/games/$GAME/seats/0
curl -s localhost:5164/api/games/$GAME
curl -s localhost:5164/api/games/$GAME/actions -H "Authorization: Bearer $TOKEN" \
     -H 'Content-Type: application/json' -d '{"kind":"roll","arguments":{"actor":"0"}}'
```

## Actions

An action is a `kind` and its `arguments`, every value a string:

```json
{ "kind": "move", "arguments": { "actor": "0", "from": "8", "to": "16" } }
```

Choose from `legalActions` and send the one you chose back unchanged: you need not understand an
action to take it, and a client that only ever echoes a legal action can never be refused for
breaking the rules. Cells and pieces are named by their ids (`cells` and `pieces` in the view); a
cell's `name` ("e4") is for people. A legal action may carry a `label` for its button; the server
ignores it on the way back.

| Kind | Arguments (besides `actor`) | What it does |
|---|---|---|
| `roll` | | Roll the dice for the turn |
| `move` | `from`, `to` (cells) | Move the piece there, taking whatever is on `to` |
| `pass` | | Move no more this turn (moving is never compulsory); after a turn, be done offering ransoms |
| `promote` | `cell`, `into` (a prisoner's piece id) | Promote the Pawn on `cell` into that piece |
| `escape` | `piece`, `home` (cell) | Let a captured piece of yours escape, back onto the board |
| `hand-over` | `regent` (seat) | Let an ally take the rest of the turn |
| `place` | `to` (cell) | Put a piece coming home from a ransom on the board |
| `offer` | `receiver` (seat), `given`, `asked` (piece ids, comma-separated) | Offer prisoners back, for others |
| `accept`, `decline` | | Answer the ransom offered |
| `propose-end` | `resigns` (`true`/`false`) | Propose ending the game; with `resigns`, give up your turns meanwhile |
| `answer-end` | `agrees` (`true`/`false`) | Answer a proposal to end the game |
| `rejoin` | | Take back the turns given up; or, having been away, say you are back |
| `vote-away` | `absent` (seat), `stops` (`true`/`false`) | Vote to stop waiting for a seat whose player is away, or to wait |
| `retire` | | Leave the game, as a seat left with only its Raja may |

Three are not among the legal actions, as they may be taken whoever's turn it is, or have too many
forms to list: build them yourself when the view says you may.

- **Ransoms**: `mayOffer` says the deciding seat may offer one. Each seat's `prisoners` are the pieces
  it holds, by id; offer some for others.
- **Ending the game**: a seat whose `mayProposeEnd` is true may propose ending it; one whose `mayRejoin`
  is true may take its turns back.
- **Waiting for someone away**: `away` in the view says who may vote, once the vote is open.

The rules refuse any of these that may not be made, as they refuse any illegal action.

## The view

`GET api/games/{id}` returns the game as any client shows it:

| Field | |
|---|---|
| `version` | How many times the game has changed: send it with `advance` |
| `decidingSeat`, `actingFor` | Whose decision it is; and, while one seat moves for another (an enemy Raja on a throne shares that seat's turn), the seat whose turn it is |
| `legalActions` | What the deciding seat may do now |
| `cells` | The board: each cell's id, where to draw it, its shape and name, and whose throne it is |
| `pieces` | Each piece: its id, kind, owner and cell |
| `seats` | Each seat: its name, team, score, who plays it, and the prisoners it holds |
| `dice` | The dice rolled this turn and not used yet, with what each lets move |
| `log` | The latest events, told for people ("Black rolled 2 (Horse) and 5 (Pawn or Raja).") |
| `outcome` | Once over: the winners, the kind of ending, and the story |
| `secondsLeft` | If the decision is timed, the time left; once it runs out, anyone may `advance` the game to decide for it |
| `promotionDue`, `returning`, `offer`, `proposal`, `away` | What the deciding seat must deal with now, if anything |

The full description of every field is in the contracts, `src/Apis/Zaturnava.Contracts`: each is a
record, documented where it is declared (`GameViewDto.cs`, `SeatDto.cs`, and so on).

## Talk at the table

The players may talk: to the whole table (`"channel": "table"`), or to their own team alone (`"team"`).
Saying something takes the seat's token, and at most 500 characters:

```json
{ "channel": "team", "text": "Hold the left; I'll take their Boat." }
```

Each message is numbered at its table, from 1, and comes back as
`{"number", "seat", "channel", "text", "at"}`. Reading takes the number of the last message you have
(`after=0` for everything) and gives what came since that you may read: everything said to the table,
and, with your token, what your team said to itself. Without a token, as a watcher, only the table. Talk is
not pushed: ask again now and then, as the page does every few seconds. Bots say nothing.

A game played again after another, created with `"continuing": "{the earlier game's id}"`, is at the same
table: its talk goes on from the earlier game's, numbered on. The talk is let go once the players have left
the table: when, for about two minutes, no player has read it or said anything (a watcher reading it does
not count). After that, it is as if nothing had been said.

## Signing in

Signing in is optional, and nothing above needs it. A person signs in from a page, in a window the page
opens, with a service the server is set up for (Proposed):

| | Request | Reply |
|---|---|---|
| The services to sign in with | `GET api/signin` | Their names, such as `["GitHub"]`; none, if none is set up |
| Sign in, in a window of its own | `GET signin/{service}?origin={the page's origin}` | The service's own page; then, once the person agrees, a page that posts `{"token": "...", "name": "..."}` to the page that opened it, and closes |
| Who a player's token stands for | `GET api/players/me`, with the player's token | `{"id": "...", "name": "..."}`, or `401` |

The page's origin must be one the server serves its page from; for any other, it answers `400`, and posts
nothing. As with a seat's, the server keeps only a player token's hash. Coming back with the same account
signs in the same player, with a new token. Seats are not yet tied to players.

## Refusals

A request the server will not do is answered with a [problem](https://www.rfc-editor.org/rfc/rfc9457)
(`application/problem+json`): its `title` is one of the refusals below, its `detail` why.

| Status | Refusal | |
|---|---|---|
| 400 | `Invalid` | The request makes no sense: an unknown ruleset or bot, a malformed action |
| 403 | `Forbidden` | The token does not hold the seat that must decide |
| 404 | `NotFound` | There is no such game |
| 409 | `Conflict` | The seat is taken, or the game changed meanwhile |
| 422 | `Illegal` | The rules do not allow the action now |

## From .NET

`Zaturnava.Client`'s `HttpGameClient` makes every one of these calls, with the contracts as types, and
turns a refusal into a `GameRequestRefusedException`.
