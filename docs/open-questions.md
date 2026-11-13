# Open questions

Every question waiting on the designer's answer, and only those: there are no others in the other documents.
When one is answered, the answer goes where it belongs (the rules, the specification, the roadmap, the code),
marked *Stated* there, and the question here is cut down to whatever is still open, or removed.

Choices marked *Proposed* elsewhere are decisions made without an answer; they stand as they are and need none.
They are marked only so that it is known which choices were the designer's and which were not, should one ever
need to change. None is a question in disguise: a choice that cannot stand without the designer's answer is
asked here instead (checked, every *Proposed* in the documents, on 2026-11-06).
Anything to be built is in the [backlog](backlog.md) or the [roadmap](grand/roadmap.md), and anything left to
playtesting is marked so where it is written.

## OQ7 A second palace

May a player buy a second palace in Grand Legions, on a board large enough, and at what price? Left open until
the rest of the game is playable ([specification Q5](grand/specification.md#9-open-questions)).

## OQ8 The price of the Tafl King's escape

The Tafl King now escapes the board from its far corner, and comes back on doubles
([Jumping and custody](grand/pieces.md#jumping-and-custody)). What should that power add to its value (71)?
Until answered, it adds nothing.

## OQ12 The worth of palace lines

The Guard (Janggi) moves as the Minister does, one cell orthogonally, but only in its palace, where it also steps
diagonally along the palace's lines. It is priced above the Minister for those lines:

| Piece | Moves | Kept to | Value |
|---|---|---|---|
| Minister | One cell orthogonally | — | 6 |
| Guard (Janggi) | One cell orthogonally; diagonally along palace lines | its palace | 7 |

Is moving along a palace's diagonals really worth more than being free to go anywhere on the board (the
designer's question of 2026-11-13)? Until answered, the prices stand.

## OQ13 Quick armies full of the cheapest pieces

Quick armies come with Advisors and Swedes far more often than other pieces (the designer's report of
2026-11-13). Measured over 2,000 armies of each size, in games whose armies may hold palace pieces:

| Size | Budget | Least army meeting the minimums | Left for choice | Advisor (2) in | Swede in | Makruk Pawn in |
|---|---|---|---|---|---|---|
| Small | 110 | about 78 | about 32, over 6 slots | 56% | 18% | 35% |
| Medium | 125 | about 93 | about 32, over 8 slots | 60% | 20% | 44% |
| Large | 200 | about 157 | about 43, over 16 slots | 96% (3.4 each) | 82% (1.3 each) | 64% |

The dealing is working as written ([QuickArmy](../src/Games/GrandLegions/Zaturnava.Games.GrandLegions/QuickArmy.cs)):
slot by slot, each piece is drawn from those its class can still pay for, leaving enough for the cheapest piece
of every slot still to fill. With so little left for choice, most slots can pay only for their class's cheapest:
the Advisor (2) among infantry, the Swede (19 on the large board) among heavy pieces, the Grasshopper among
artillery. A slot drawn early may spend most of what is left, so the slots after it get the cheapest.

Which should change?

- the large game's budget (the minimums cost about 157 of its 200, at the large board's prices), or its minimums;
- the dealing: share what is left more evenly between the slots, so that later ones are not left the cheapest,
  at the cost of the dearest pieces (the Queen, the Amazon) coming hardly ever;
- or nothing: an army of many cheap pieces is what such a budget buys.
