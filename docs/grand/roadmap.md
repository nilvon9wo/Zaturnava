# The grand game — Roadmap

How the [grand game](specification.md) is to be built, in stages that can each be played, or at
least tested, on their own. As with the classic game, it is played locally against bots before it
is hosted. The classic game stays playable, and its tests passing, at every stage.

Nothing here is scheduled; the order is a proposal. Each stage names the open questions
([specification §9](specification.md#9-open-questions)) it waits on.

## Stage 0 — Agree the design

- Settle the open questions, first those that shape the engine: dice (**Q1**), leaders (**Q5**),
  scoring and the purse (**Q12**, **Q13**), promotion (**Q14**).
- Settle the sheet's [review notes](pieces.md#review-notes) (**Q22**).
- Decide whether the spreadsheet is committed, as the catalogue's source, or the catalogue is kept
  in Markdown (or as data in the code) and the sheet kept privately.
- Choose a name, or keep "grand" as the working name of the variant's project and key.

## Stage 1 — The catalogue as data

A new variant project (working name `Zaturnava.Variants.Grand`), holding every piece of
[pieces.md](pieces.md) as data. The engine's movement grammar (`MovementRule`: step, slide, leap,
move-only, capture-only) already covers most of them. It grows to cover the rest, each a few
lines of data once the grammar knows how:

- **Lame leapers**: Horse, the Janggi and Xiangqi Elephants (a leap blocked by a cell on the way).
- **Hoppers**: the Cannons, the Grasshopper (a move or capture that needs a piece to jump).
- **Riders**: the Knightrider (a slide by leaps).
- **Area rules**: palace, river, colour — kept as a piece's restrictions, so that they can be lifted.

The piece-information card (§8 of the specification) can come here too, and serve the classic game
at once.

Waits on: **Q9**, **Q20**, **Q21**, **Q22**.

## Stage 2 — The board

- Any even width and length; four kingdoms; rivers drawn on the board.
- Same-side and opposite-corner arrangements, with forward for each (**Q3**, **Q8**).
- A team game or everyone for themselves (`TeamGraph` already allows both).
- The lobby's game-creation form, with "at random" for every setting (**Q2**).
- To be playable before the draft exists: each army a fixed one, set by the program.

## Stage 3 — Palaces, thrones and leaders

- A palace (3 × 3) and its throne, per army; palace lines drawn, and the move toward the throne
  (**Q10**); confinement to a palace (**Q11**).
- Several leaders in one army: throne control, and everything the classic rules say of "the Raja",
  said of leaders (**Q1**, **Q5**).

## Stage 4 — The draft

- A setup phase before play: each player drafts within the budget, buys lifts, places their palace,
  throne and pieces (**Q6**, **Q7**); a "fill at random" for each step.
- Infantry losing start-row powers when placed beyond row 2 (**Q4**).
- Bots draft too: Random at random, Midas by value, Solomon… evenly.

## Stage 5 — Points to spend

- Upgrades and promotions bought in play, in a palace with the player's own leader on its throne
  (**Q13**, **Q14**).
- The four-throne bonus (**Q15**).
- Bots that spend.

## Stage 6 — Special powers

- Start-row powers: the two-square first move, *en passant*.
- Castling (**Q18**, **Q19**).
- The flying-general capture (**Q16**).
- The palace face-off (**Q17**): a decision out of turn, which the turn machine handles as it
  handles a ransom window.
- Each promotion by its own condition (Shogi's far three ranks, Makruk's sixth rank, the Western
  last rank).

## Stage 7 — Looks

- Every kind its own look, in both piece sets; the class told at a glance (spec §8).
- Playtest, as the classic game was, and act on what it asks for.

## Later

- More kingdoms than four, or of unequal size.
- More pieces.
