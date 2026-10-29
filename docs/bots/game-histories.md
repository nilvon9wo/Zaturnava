# Game histories

A game's history is everything an AI needs to learn from it: who played each seat, every action in
order, the final scores, and how the game ended. The server gives the history of any game it keeps,
and the BotRunner writes every game it plays in the same shape, so games against people and games of
[self-play](self-play.md) can be learned from together.

| | |
|---|---|
| One game, going on or over | `GET api/games/{id}/history` (MCP: `game_history`) |
| Every finished game the server keeps | `GET api/history` |
| Games bots played offline | `Zaturnava.BotRunner --out games.jsonl`: one history per line |

A server keeps games as long as its storage does: in memory, until it restarts, unless it is given a
database ([running the game](../contribute/local-development.md#running-the-game)).

## The shape

```json
{
  "format": 1,
  "gameId": "4f1c...",
  "rulesetId": "chaturaji-2027",
  "seats": [
    { "id": 0, "name": "Black", "team": 0, "player": "person", "score": 23 },
    { "id": 1, "name": "Yellow", "team": 1, "player": "sage", "score": 9 },
    { "id": 2, "name": "Green", "team": 0, "player": "argus", "score": 14 },
    { "id": 3, "name": "Red", "team": 1, "player": "sage", "score": 11 }
  ],
  "actions": [
    { "kind": "dice", "arguments": { "actor": "0", "values": "1" } },
    { "kind": "dice", "arguments": { "actor": "1", "values": "6" } },
    { "kind": "move", "arguments": { "actor": "2", "from": "53", "to": "45" } },
    { "kind": "roll", "arguments": { "actor": "0" } },
    { "kind": "dice", "arguments": { "actor": "0", "values": "5,6" } },
    { "kind": "move", "arguments": { "actor": "0", "from": "8", "to": "16" } }
  ],
  "outcome": {
    "winners": [0, 2],
    "ending": "agreement",
    "reason": "every player agreed to end it, so points decide"
  }
}
```

| Field | |
|---|---|
| `format` | `1`; it changes only if the shape does |
| `gameId` | The server's id for the game; `null` for one played offline |
| `seats` | Each seat: its number, name and team, who played it (`person`, `open` for a seat never claimed, or the bot's id), and its score when the history was taken |
| `actions` | Every action, in order, as [the HTTP API](http-api.md#actions) describes them (the example above is cut short). The dice are among them: each die of the roll-off for who goes first, and each roll, is a `dice` action with the `values` it came up, so replaying them is exact |
| `outcome` | `null` while the game goes on, or for an offline game that reached its step limit; else the seats that won (none for a draw), the kind of ending, and why |

A game started part-way, from a scenario (on a development server), begins with a `scenario` action
that names it.

## How a game ends

Who won is not all there is to learn: a team that wins by agreement, 31 to 29, played a different game
from one that took every enemy piece. The `ending` says which:

| `ending` | | Who wins |
|---|---|---|
| `one-team-left` | Only one team has pieces left | That team, outright |
| `only-rajas-left` | Only Rajas are left: an honourable peace | No one: a draw |
| `agreement` | Every player still playing agreed to end it | The team with more points (a draw if level) |
| `team-gave-up` | A whole team gave up its turns, trailing (or far ahead) | The team with more points |
| `idle` | No one rolled or moved for some rounds | The team with more points |

The scores, with the ending, say by how much. [rules.md §10](../rules.md) has the rules behind each.

## Seeing every position

A history holds the actions, not the positions between them: replay the actions through the ruleset
to see each. In .NET, with `Zaturnava.Games.Chaturaji2027`:

```csharp
IRulesApplying rules = new Chaturaji2027Rules();
GameState state = rules.CreateInitialState();
foreach (ActionDto action in history.Actions)
{
    GameAction parsed = rules.Parse(new ActionDescriptor(action.Kind, action.Arguments));
    state = rules.Apply(state, parsed).State;
    // state is the position after this action: its pieces, scores, dice, prisoners, whose turn...
}
```

In another language, you would have to implement the rules yourself; or have your AI learn from the
views it is shown while it plays (`GET api/games/{id}`), which describe each position in full.
