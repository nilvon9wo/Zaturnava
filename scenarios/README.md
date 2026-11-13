# Scenarios

Games set up part-way, to try a corner of the rules without playing up to it:
a back-to-back swap, a promotion, an escape, every Raja on a throne, a Checkers
jump chain, Tafl custody... A server in development
(`appsettings.Development.json`, `Scenarios:Folder`) offers each `.json` file in
this folder, and those below it, in the lobby's **Start from** list, under
**Custom**: those of the game and size chosen. No other server offers any.

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
- `edition`: the edition of the ruleset's pieces and rules it is played by
  (Grand Legions: 6 plays a chain of jumps jump by jump); the first, 0, if left
  out.

A piece may be of any kind the ruleset plays, whether or not a new game has
one. Each piece takes the identity of a piece of its seat and kind in a new
game, in turn, so that it goes home where that piece started (a ransom, an
escape). Pieces beyond what a new game has get identities of their own: they
start nowhere, so a Western Pawn among them has no two-square first step, and
a Shogi piece among them is a mercenary by its own kind.

## Grand Legions' medium board

`grand-legions-medium`: 8 × 8, columns `a` to `h` left to right, rows `1` to
`8` bottom to top. Black's army faces up, as in Chaturaji 2027; Black and
Green are a team, and so are Yellow and Red. Pieces are named as the game
names them: `Black King (Checkers)`, `Red Rook (Western)`, `Black Man`. Dice:
1 artillery, 2 cavalry, 3 or 4 heavy, 5 or 6 infantry or leader.

| Scenario | What it tries |
|---|---|
| `castling` | The Western King castling |
| `generals` | The flying general's capture, and the face-off |
| `cannons` | The Xiangqi and Janggi Cannons, and the Manchu Chariot |
| `checkers-chain` | Jump chains, jump by jump, stopping after any |
| `tafl-custody` | Custody: three pieces taken at once |
| `tafl-escape` | The Tafl King leaving the board from its far corner |
| `promotions` | A Pawn's own promotion, the far edge's, and the Shogi far three rows |
| `mercenaries` | Hiring a captured Shogi piece |
| `leapers` | Blocked leaps (Xiangqi Horse, Janggi Elephant) and the Grasshopper |
| `pawns` | The Berolina Pawn, and Pawns back to back swapping |

None has palaces or rivers, which a scenario cannot set yet: the palace powers
(palace lines, pieces confined to a palace) and the river pieces are left to a
dealt game.

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
