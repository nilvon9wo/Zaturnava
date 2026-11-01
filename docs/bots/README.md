# Bring your own AI

Zaturnava's game server plays anyone who can make an HTTP request: a person in a browser, one of the
server's own bots, or a program of yours. You can seat your own AI in a game against people or
against our bots, download the games played to learn from, and train it offline against our bots,
as many games as you like, without a server at all.

| You have | Start with |
|---|---|
| A program to play on the live server, against the people on itch.io | [The live server](live-server.md): where it is, a first game, and what is protected |
| An AI assistant that speaks MCP (Claude Code, Claude Desktop, any MCP client) | [MCP](mcp.md): give it the tools, and ask it to play |
| A program in any language | [The HTTP API](http-api.md): create or join a game, read it, act |
| A bot written in .NET, to train or measure | [Self-play](self-play.md): your bot against ours, thousands of games, offline |
| A learner that wants data | [Game histories](game-histories.md): every action of every game, with the scores and how it ended |

To seat your AI in a game you set up yourself, choose **Custom** in the lobby and, for its seat, **Your own
AI**: the seat stays open, and while any seat is open the table shows the game's id. Your AI takes the seat
with that id and an AI token, made under **New AI token** in your profile (see [the HTTP API](http-api.md)).

Whichever way you connect, your AI sees exactly what a person sees and may do exactly what a person
may: the server tells it which actions are legal, and refuses any other. Nothing it does needs a key
of ours, and whatever AI service it uses is between you and that service: its keys stay with you.

## The game

The rules are in [rules.md](../rules.md): four seats in two teams, an 8x8 board, dice that say which
piece may move. The ruleset's id is `chaturaji-2027`. The [architecture](../architecture.md)
explains how the engine is built, should you want to run it yourself (for self-play, or to replay a
game's history and see every position).

## Running a server

The live server, <https://zaturnava.runasp.net/>, is the one the game on itch.io plays on: see
[the live server](live-server.md). To train, to play many games, or to try something out, run one yourself: see
[running the game](../contribute/local-development.md#running-the-game). It listens on
<http://localhost:5164/>; the page, on <http://localhost:5165/>, shows the games your AI plays, so
you can watch it, or play with it.

## Our bots

The server's own bots are listed by `GET api/catalog`, and described in the game under "The bots":
Random, Midas (greedy), Solomon (careful), Argus (watchful) and Sage (looks ahead through the dice,
its weights tuned by self-play). Their ratings against each other are there too. Beating Sage is a
fair first goal.
