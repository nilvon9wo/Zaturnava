# Scenarios

Games set up part-way, to try a corner of the rules without playing up to it:
a back-to-back swap, a promotion, an escape, every Raja on a throne... A server
in development (`appsettings.Development.json`, `Scenarios:Folder`) offers each
`.json` file in this folder, and those below it, in the lobby's **Start from**
list. No other server offers any.

The files are read whenever the lobby asks, so a new or changed file shows up
on reloading the lobby, without restarting the server. A game keeps the
scenario as it was when the game began. A file that cannot be read is listed
all the same, saying why; one that names what its ruleset lacks is refused,
saying what, when a game is started from it. Every scenario here is started in
a test (`Create_WhenFromEachScenarioOfTheRepository_StartsThere`), so a broken
one fails the build.

## Writing one

The file's name is its id. Everything is written as the game writes it, in
any case: cells as `d4`, seats as `Black`, pieces as the seat then the kind,
`Black Pawn`.

```json
{
  "rulesetId": "chaturaji-2027",
  "name": "Back to back",
  "description": "What it is for, shown under the list when chosen.",
  "turn": "Black",
  "dice": [5, 3],
  "pieces": { "d4": "Black Pawn", "d5": "Green Pawn", "d1": "Black Raja" },
  "prisoners": [{ "piece": "Yellow Elephant", "holder": "Black" }],
  "scores": { "Black": 4 }
}
```

- `rulesetId`, `name` and `description` are required.
- `pieces`: every piece on the board, by its cell. Nothing else is.
- `turn`: whose turn it is; the first seat's, if left out. There is no
  roll-off.
- `dice`: what that turn has rolled, not used yet; left out, the turn starts
  with its roll. The dice are otherwise random.
- `prisoners`: pieces captured, and who holds them.
- `scores`: points so far; none for a seat left out.

Each piece takes the identity of a piece of its seat and kind in a new game,
in turn, so that it goes home where that piece started (a ransom, an escape).
Pieces beyond what a new game has get identities of their own.

## Chaturaji 2027's board

Columns `a` to `h` run left to right, rows `1` to `8` bottom to top.

| Seat | Raja (throne) | Elephant | Horse | Boat | Pawns | Pawns move |
|---|---|---|---|---|---|---|
| Black | d1 | c1 | b1 | a1 | a2–d2 | up |
| Yellow | a5 | a6 | a7 | a8 | b5–b8 | right |
| Green | e8 | f8 | g8 | h8 | e7–h7 | down |
| Red | h4 | h3 | h2 | h1 | g1–g4 | left |

Black and Green are a team, and so are Yellow and Red. Dice: 1 Boat, 2 Horse,
3 or 4 Elephant, 5 or 6 Pawn or Raja.
