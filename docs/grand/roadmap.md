# The grand game — Roadmap

How the [grand game](specification.md) is to be built, in stages that can each be played, or at
least tested, on their own. As with the classic game, it is played locally against bots before it
is hosted. The classic game stays playable, and its tests passing, at every stage.

Nothing here is scheduled; the order is a proposal. Each stage names the open questions
([specification §9](specification.md#9-open-questions)) it waits on.

## Stage 0 — Agree the design

- Settle the open questions, first the catalogue's last notes (**Q20**).
- Choose a name, or keep "grand" as the working name of the variant's project and key.

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

The piece-information card (§8 of the specification) can come here too, and serve the classic game
at once.

Waits on: **Q7**, **Q19**, **Q20**.

## Stage 2 — The board and the dice

- A board and a kingdom size (at least 3 × 3), set apart; a kingdom in each corner, lying as the
  game is set (pinwheel, or top and bottom); rivers drawn on the board.
- Same-side and opposite-corner arrangements, with forward for each (**Q2**, **Q6**).
- A team game or everyone for themselves (`TeamGraph` already allows both).
- Dice that name a class: 1 Artillery, 2 Cavalry, 3–4 Heavy, 5–6 Infantry or Leader.
- The lobby's game-creation form, with "at random" for every setting (**Q1**).
- To be playable before the draft exists: each army a fixed one, set by the program.

## Stage 3 — Palaces, thrones and leaders

- A palace (3 × 3) and its throne, per army; palace lines drawn, and the move toward the throne (**Q8**); confinement to a palace (**Q9**).
- Several leaders in one army: throne control, and everything the classic rules say of "the Raja",
  said of leaders.
- Scoring by piece value, with the classic bonuses as they are until playtesting says otherwise
  (**Q14**); the game ends as the classic game does, and points decide it.

## Stage 4 — The draft

- A setup phase before play: each player drafts within the budget, buys lifts, places their palace,
  throne and pieces, in secret or in turn as the game is set (**Q4**, **Q5**); a "fill at random"
  for each step. Points not spent start the player's score.
- Infantry losing start-row powers when placed beyond row 2 (**Q3**).
- Bots draft too: Random at random, Midas by value, Solomon… evenly.
- Later: prices for each game, from its own board and kingdoms
  ([pieces.md](pieces.md#prices-for-each-game-proposed)).

## Stage 5 — Points to spend

- Upgrades and promotions bought in play, in a palace with the player's own leader on its throne,
  lowering the score.
- Promotion by a piece's own condition, free; Infantry promoting at the far edge, into its own
  promotion or any captured piece; every promotion optional, and able to wait while its condition
  holds.
- The four-throne bonus (**Q12**).
- Bots that spend.

## Stage 6 — Special powers

- Start-row powers: the two-square first move, *en passant*.
- Castling (**Q17**, **Q18**).
- The flying-general capture (**Q15**).
- The palace face-off (**Q16**): a decision out of turn, which the turn machine handles as it
  handles a ransom window.

## Stage 7 — Looks

- Every kind its own look, in both piece sets; the class told at a glance (spec §8).
- Playtest, as the classic game was, and act on what it asks for: among other things, the classic
  bonuses and the leader bonus of 60 (**Q14**).

## Later

- More kingdoms than four, or of unequal size.
- More pieces.
- A second palace, if one may be bought (**Q5**).
- Kingdoms smaller than 3 × 3, with the palace partly outside the kingdom.
- Games without palaces: bare thrones, and no palace-bound pieces in the draft.
