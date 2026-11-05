# The grand game — Roadmap

How the [grand game](specification.md) is to be built, in stages that can each be played, or at
least tested, on their own. As with Chaturaji 2027, it is played locally against bots before it
is hosted. Chaturaji 2027 stays playable, and its tests passing, at every stage.

The order is agreed (2026-10-26); nothing is scheduled. The design's questions are answered, but
for a second palace (**Q5**, for later), versions of saved settings (**Q26**, for later) and what
playtesting is to settle (**Q14**)
([specification §9](specification.md#9-open-questions)).

## Stage 0 — Finish what the grand game inherits

The grand game plays by the Chaturaji 2027 rules, so the rules it inherits come first, built once,
and written for a *leader* rather than a Raja, so that they carry over unchanged.

- **Smarter bots**: before moving, a bot weighs what its opponents could take in reply (and how
  the exchange on that cell would end), and counts a piece left unguarded, or a throne under threat,
  against what the move gains. Still one move deep, so still fast on a large board; looking further
  ahead, across the dice, comes later. Every playtest of either game needs this. *Done*: Argus.
- Escape on doubles (rules.md §7), the empire bonus (§5), voluntary regency (§6.2). *Done*: all three.
- Ending the game by agreement (§10). *Done*.

Not first, and blocking nothing: boat triumph (Chaturaji 2027 only, and a near-impossible condition, so a
nice-to-have); games that survive a restart, identities, statistics and chat (part of going
online, not of either game's rules).

The grand game is named **Grand Legions** (chosen 2026-10-31).

## Stage 1 — The catalogue as data

A new variant project (working name `Zaturnava.Variants.Grand`), holding every piece of
[pieces.md](pieces.md) as data, with its class. The engine's movement grammar (`MovementRule`:
step, slide, leap, move-only, capture-only) already covers most of them. It grows to cover the
rest, each a few lines of data once the grammar knows how:

- **Lame leapers**: Horse, the Janggi and Xiangqi Elephants (a leap blocked by a cell on the way).
- **Hoppers**: the Cannons, the Grasshopper (a move or capture that needs a piece to jump).
- **Riders**: the Knightrider (a slide by leaps).
- **Area rules**: palace, river, colour — kept as a piece's restrictions, so that those that may be
  bought off can be.

The piece-information card (§8 of the specification) can come here too, and serve Chaturaji 2027
at once.

Nothing here waits on an open question.

*Done (2026-10-31)*: the grammar's leapers, lame leaps, hoppers and riders
([architecture §2.2](../architecture.md#22-piece-catalog-and-movement-grammar)), and the catalogue in
`Zaturnava.Games.GrandLegions` (`GrandLegionsPieces`), each piece's reach checked against its value. Still
to come: the area rules (palace, river), which belong with the board (Stage 2) and the palaces
(Stage 3); and the piece-information card.

## Stage 2 — The board and the dice

- A board and a kingdom size (at least 3 × 3), set apart; a kingdom in each corner, lying as the
  game is set (pinwheel, or top and bottom); rivers drawn on the board in blue, from the first
  version with rivers in it.
- Forward away from each kingdom's back rank.
- Rivers between or through the middle cells, through or around kingdoms, and what they mean to the
  pieces that care.
- A team game, with any two seats allied, picked on a picture of the four seats; or everyone for
  themselves.
- Dice that name a class: 1 Artillery, 2 Cavalry, 3–4 Heavy, 5–6 Infantry or Leader.
- The lobby's game-creation form, with "at random" for every setting; boards up to 26 × 26.
- To be playable before the draft exists: each army a fixed one, set by the program.

*Begun (2026-10-31)*: the board as a game is set (`GrandBoard`): its kingdoms in their corners, by
pinwheel or top and bottom, each with its forward; whether the setting fits (sides up to 26, kingdoms
at least 3 × 3, none overlapping); which cells a river cuts, and which are across it from a kingdom,
with the river through or around kingdoms. The dice (`ClassDice`); teams (`Alliances`). Still to come:
what rivers mean to the pieces, the lobby's form (with the allies' picture), fixed armies, and the
ruleset that plays them.

*Playable (2026-11-01)*: quick games, as the game's author set them, offered in the lobby's Game list
beside Chaturaji 2027, each at easy, medium or hard:

| Size | Board | Kingdom | Budget | At least |
|---|---|---|---|---|
| Small | 6 × 6 | 3 × 3 | 110 | 1 leader, 1 heavy, 1 cavalry, 3 infantry |
| Medium | 8 × 8 | 4 × 2 | 125 | 1 leader, 1 heavy, 1 cavalry, 1 artillery, 4 infantry |
| Large | 12 × 12 | 6 × 3 (6 along the edge, 3 deep, a pinwheel as in Chaturaji) | 200 | 1 leader, 3 heavy, 2 cavalry, 2 artillery, 8 infantry |

How a quick game's armies are chosen and placed (*Stated*, 2026-11-01 and 2026-11-04):

- **Each army apart.** Each seat's army is picked on its own, at random, so the four differ, and
  differ from game to game. (A setting may give all four one army, chosen at random or by the
  creator; never the default.) Pieces are picked one at a time, at random, from the most expensive
  classes down: the leader first (a quick game has exactly one), then heavy, artillery, cavalry, and
  infantry last, until the minimums are met. If the budget cannot buy the minimum army, the dearer
  pieces are exchanged for cheaper ones until it can. With budget and room in the kingdom left, more
  pieces, of any class; with budget still left, lifts, at random across the army.
- **Pieces an area holds.** A piece that depends on palaces is picked only in a game with palaces; one
  that depends on a river (the Xiangqi Elephant and Soldier), only in a game with a river. This holds
  for armies picked at random: a player building their own army may buy them regardless, though they
  are no priority until palaces and rivers, respectively, are in the game.
- **Placing them.** Where the kingdoms touch, with no ground between them (the small game: two
  kingdoms of 3 fill a side of 6), the leader stands on its throne at the board's outer corner. In
  every other quick game, the leader, heavy, artillery and cavalry are placed in the back rank(s) in a
  random order, the leader on its throne; infantry in random order in front of them. So each army is
  laid out differently, unless the creator chose a symmetrical game. In a game not quick, each player
  places all their pieces.
- **The rules.** Every Chaturaji 2027 rule applies (rules.md), but the Boat triumph, as there are no
  boats; each piece moves as the catalogue describes it, which may or may not map easily onto Chaturaji
  2027's. One leader, if an army has any, starts on the throne. Every kind of infantry may be promoted
  into any captured piece at the far edge, besides any promotion of its own. The Chaturaji 2027
  bonuses are × 10 (specification §6.3).
- **Looks.** Every piece has a symbol, shared if need be, with letters over it to tell apart pieces
  that share one, so that every piece can always be told from every other by its symbol and letters
  together. First letters alone are not enough: they are not unique.

What the program does so far (2026-11-04), and the gaps against the above:

- Each game is dealt its own armies: its first action carries a seed (`ScenarioStart.Dealt`), from which
  `QuickArmy` picks and places each seat's army apart, at random, as above; each leader's start is its
  throne, and each piece's its home for ransom. Games begun before this replay as the first seed deals
  them. The order of the middle classes (heavy, artillery, cavalry) and how the dearest pieces are made
  cheaper (each time, the piece dearest beyond its class's cheapest, one step down) are *Proposed*.
- Palace pieces are left out, as quick games have no palaces; river pieces, unless the game has rivers
  (each area restriction says which it is about: `Terrain`).
- *Gap*: lifts are not bought with what is left of the budget, as nothing plays a lifted restriction yet.
- *Gap*: there is no setting yet for all four armies alike, or laid out symmetrically.
- The Chaturaji 2027 bonuses are × 10, as is the lead a team needs to win by giving up its turns
  (*Proposed*: 100), each read from a table (`Scoring/grand-legions.json`; Chaturaji 2027's own in
  `Scoring/chaturaji-2027.json`) that can be changed apart from the code. Piece values, each piece's
  price and what taking it scores, are read from tables too (`Values/grand-legions.json`,
  `Values/chaturaji-2027.json`), as specification §6.3 asks.
- The board has no river in a quick game yet; a river keeps a Xiangqi Elephant on its own side
  (`RiverCrossing`). The Xiangqi Soldier's sideways step once across is still to come. Until quick
  games have rivers, they have no river pieces.
- The Medium kingdom, 4 × 2, is below the 3 × 3 a palace needs; quick games have no palace, so it
  plays, but a Medium game with palaces will need the palace to stand partly outside the kingdom
  (specification §5), or none.
- Pieces are drawn by their class's emblem with letters telling them apart (#146).
- Still to come: choosing a quick setup for one's own kingdom in a game set up by hand, which waits for
  the draft (Stage 4).

Saved settings ([specification §2.1](specification.md#21-saved-settings)), named, shared, and copied
to edit, wait for players to sign in and for storage that lasts.

## Stage 3 — Palaces, thrones and leaders

- A palace (3 × 3) and its throne, per army; palace lines drawn, and steps along them;
  confinement to a palace.
- Several leaders in one army: throne control, and everything the Chaturaji 2027 rules say of "the Raja",
  said of leaders.
- Scoring by piece value, with the Chaturaji 2027 bonuses multiplied by 10 until playtesting says
  otherwise (**Q14**); the game ends as Chaturaji 2027 does, and points decide it.

## Playtest — fixed armies

Before the draft is built: a playtest of stages 1–3, with armies the program sets, and each piece
drawn simply, a shape for its class and a letter for its kind.

## Stage 4 — The draft

- A setup phase before play: each player places their palace and throne, then drafts within the
  budget, buying lifts and placing each piece, in secret or in turn as the game is set; a "fill at
  random" for each step. Points not spent start the player's score.
- Infantry placed beyond the start rows (2, or as the game is set), without start-row powers.
- Pieces confined to a palace placed in it.
- Bots draft too: Random at random, Midas by value, Solomon… evenly.
- Later: prices for each game, from its own board and kingdoms
  ([pieces.md](pieces.md#prices-for-each-game-proposed)).

## Stage 5 — Points to spend

- Upgrades and promotions bought in play, in a palace with the player's own leader on its throne,
  lowering the score.
- Promotion by a piece's own condition, free; Infantry promoting at the far edge, into its own
  promotion or any captured piece; every promotion optional, and able to wait while its condition
  holds.
- The four-throne bonus, once for each set of leaders.
- Bots that spend.

## Stage 6 — Special powers

- Start-row powers: the two-square first move, *en passant*, which may take any enemy piece that
  moves forward two squares or more past the pawn, from two squares away (specification §3). *Done*
  (2026-11-05) for the Western Pawn, with start rows 2 (`StartRowMoves`); its special-power bonus is
  3, its value 9 (*Proposed*, [pieces.md](pieces.md#value)). Still to come: start rows as a setting.
- Castling.
- The flying-general capture.
- The leader face-off: a decision out of turn, open for as long as the two leaders face each other;
  a flashing button while it is open, and optionally a pause when it first opens (specification §6.4).

## Stage 7 — Looks

- Every kind its own look, in both piece sets; the class told at a glance (spec §8).
- Playtest, as Chaturaji 2027 was, and act on what it asks for: among other things, the Chaturaji 2027
  bonuses, the leader bonus of 60, and the Pawns' promotion premiums
  ([pieces.md](pieces.md#still-open)) (**Q14**).

## Later

- More kingdoms than four, or of unequal size.
- More pieces.
- A second palace, if one may be bought (**Q5**).
- Kingdoms smaller than 3 × 3, with the palace partly outside the kingdom.
- Games without palaces: bare thrones, and no palace-bound pieces in the draft.
