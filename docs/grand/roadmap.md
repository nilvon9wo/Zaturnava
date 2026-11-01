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

Choose a name for the grand game at any point, or keep "grand" as the working name of its project
and key.

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

*Proposed*, for the author to confirm or change:

- The program picks each army (`QuickArmy`), the same for all four seats: the cheapest pieces that meet
  the minimums, then class by class (heavy, cavalry, artillery, infantry, the leader last) the best
  piece the rest of the budget buys, a piece not yet in the army first; then, while the kingdom has
  room, the best affordable pieces but no second leader. Pieces an area holds (palace, river) are left
  out until palaces come. The leader takes the back rank's inner corner, as the Raja does; heavy,
  artillery and cavalry the rest of the back ranks; infantry the front. Where the kingdoms touch, with no
  neutral ground between them (the small game: two kingdoms of 3 fill a side of 6), the leader takes the
  far corner instead, so that it is likely to outlive the first moves (*Stated 2026-11-01*). An army the
  program lays out for a custom game will follow the same rule.
- The quick games are played by Chaturaji 2027's own rules, generalised (`Chaturaji2027Variant`):
  each leader is what they call a Raja and its start its throne; the Western, Makruk and Shogi Pawns are
  their Pawns, promoted at the far edge into a captured piece; a die moves any piece of the class it
  names; taking a piece scores its value, with the Chaturaji 2027 bonuses as they are (not yet × 10).
- The board has no river in a quick game; a river keeps a Xiangqi Elephant on its own side
  (`RiverCrossing`). The Xiangqi Soldier's sideways step once across is still to come.
- The Medium kingdom, 4 × 2, is below the 3 × 3 a palace needs; quick games have no palace, so it
  plays, but a Medium game with palaces will need the palace to stand partly outside the kingdom
  (specification §5), or none.
- Pieces with no symbol of their own show the first letter of their name.
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

- A setup phase before play: each player drafts within the budget, buys lifts, places their palace,
  throne and pieces, in secret or in turn as the game is set; a "fill at random"
  for each step. Points not spent start the player's score.
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

- Start-row powers: the two-square first move, *en passant*.
- Castling.
- The flying-general capture.
- The leader face-off: a decision out of turn, which the turn machine handles as it
  handles a ransom window.

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
