# The grand game — Pieces

The pieces of the [grand game](specification.md), as listed in the designer's spreadsheet,
`docs/Other Potential Pieces.xlsx`, most valuable first within each class. The spreadsheet is the source; this page is a readable copy of it, and the two are changed
together.

## Value

A piece's value is the sum of the sheet's bonus columns; the sheet enters each by hand (it has no
formulas):

| Part | As entered |
|---|---|
| Leader bonus | 60 for a leader, else 0 |
| Direction bonus | usually the number of directions it may move in (1 direction: 0) |
| Speed bonus | its greatest distance on an 8 × 8 board; 0 for a piece that moves one square |
| Promotion bonus | what its promotion is worth (Western Pawn 5, Shogi Pawn 4…) |
| Special-power bonus | what its powers are worth (1 to 3) |
| Limitation devaluation | −1 or −2 for each kind of restriction |

Speed is counted on an 8 × 8 board even when the board is larger, so that it stays bounded; a
power that reaches far counts as speed (the Xiangqi General's flying-general capture gives it a
speed of 7, though it steps one square).

### Binding

Some pieces can never leave part of the board, whatever is paid: a piece that moves only diagonally
keeps to one colour, half the board; the War Engine keeps to a quarter, the Alfil and the Xiangqi
Elephant to an eighth, the Threeleaper to a ninth. Whatever stands on the rest, a leader above all,
is safe from it. Such a piece's limitation devaluation is set by the share of the board it can
reach, apart from any other restriction it has:

| Reaches | Devaluation | Pieces |
|---|---|---|
| a half | −2 | Bishop, Camel, Courtier |
| a quarter | −3 | War Engine |
| an eighth or less | −4 | Alfil, Xiangqi Elephant, Threeleaper |

The Xiangqi Elephant keeps its −2 for its blocked leg and the river besides, and the Advisor its −2
for its palace, which binds it more than its colour does. A bound piece is not worthless: two Bishops on opposite colours cover the board
between them, which is why the devaluation is a few points and not a fraction of the value.

The value is what the piece costs in the draft, what a promotion into it costs in play, and what
capturing it scores ([specification §6.2](specification.md#62-points-scoring-and-spending)).

## Catalogue

"Lifts" gives the cost of lifting the movement restriction and the area restriction, in that
order; "—" means there is none to buy. A restriction that the piece's own move imposes (a piece
that moves only diagonally never leaves its colour) cannot be bought off, and has no lift.

The dice name a class, not a piece ([specification §6.1](specification.md#61-the-classic-rules)).

### Leaders

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Dragon King (Shogi) | Any distance orthogonally, or one square diagonally | — | — | 75 | — / — |
| General (Xiangqi) | One square orthogonally | Area: its 3 × 3 palace | Flying-general capture | 72 | — / 5 |
| Fairy General (custom) | One square in any direction, or leaps 2+1 | — | — | 71 | — / — |
| King (Western) | One square in any direction | — | Castling | 70 | — / — |
| Silver General (Shogi) | One square diagonally, or straight forward | — | → Gold General in the far three ranks | 68 | — / — |
| General (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace face-off; palace lines | 68 | — / 5 |
| Gold General (Shogi) | One square orthogonally, or diagonally forward | — | — | 66 | — / — |

### Infantry

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Pawn (Western) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | First move two squares; *en passant*; → Queen, Rook, Bishop or Knight on the last rank | 8 | 2 / — |
| Noble (Makruk) | One square diagonally, or straight forward | — | — | 5 | — / — |
| Guard (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace lines | 7 | — / 5 |
| Soldier (Janggi) | One square forward or sideways (diagonally forward along palace lines) | Move: may not go back; Area: palace diagonals only | Palace lines | 5 | — / 2 |
| Pawn (Shogi) | One square forward | Move: blocked straight ahead | → Gold General in the far three ranks | 3 | 2 / — |
| Pawn (Makruk) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | → Courtier on the sixth rank | 3 | 2 / — |
| Advisor (Xiangqi) | One square diagonally | Area: its 3 × 3 palace | — | 2 | — / 5 |
| Soldier (Xiangqi) | One square forward, or sideways | Move: no sideways before crossing the river, never back | — | 2 | — / 3 |

Any Infantry piece that reaches the far edge may also be promoted into any captured piece, as in
the classic game.

### Heavy / Slider

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Amazon | Any distance in any direction, or leaps 2+1 | — | — | 17 | — / — |
| Queen (Western) | Any distance in any direction | — | — | 15 | — / — |
| Archbishop | Any distance diagonally, or leaps 2+1 | — | — | 15 | — / — |
| Chancellor | Any distance orthogonally, or leaps 2+1 | — | — | 15 | — / — |
| Dragon Horse (Shogi) | Any distance diagonally, or one square orthogonally | — | — | 15 | — / — |
| Dragon Bishop (custom) | Any distance diagonally, or leaps 2+1 | — | — | 15 | — / — |
| Rook (Western) | Any distance orthogonally | — | — | 11 | — / — |
| Bishop (Western) | Any distance diagonally | One colour (by its move) | — | 9 | — / — |
| Lance (Shogi) | Any distance straight forward | Move: blocked by pieces in its path | → Gold General in the far three ranks | 9 | 2 / — |

### Cavalry / Leaper

| Piece | Moves | Restrictions | Value | Lifts |
|---|---|---|---|---|
| Knightrider | Repeats 2+1 leaps in one direction | Move: each landing cell short of the last must be empty | 16 | — / — |
| Giraffe | Leaps 4+1 | — | 13 | — / — |
| Zebra | Leaps 3+2 | Area: one colour | 12 | — / 3 |
| Camel | Leaps 3+1 | One colour (by its move) | 10 | — / — |
| Knight | Leaps 2+1 | — | 11 | — / — |
| Horse | Leaps 2+1 | Move: the first square of the two-square leg must be empty | 10 | 2 / — |
| Elephant (Janggi) | One orthogonal step, then two diagonal steps outward | Move: each cell on the way must be empty | 10 | 2 / — |
| Elephant (Alfil) | Leaps 2+2 diagonally | One cell in eight (by its move) | 4 | — / — |
| Threeleaper | Leaps three squares orthogonally | One cell in nine (by its move) | 3 | — / — |
| Elephant (Xiangqi) | Two squares diagonally | Move: the square between must be empty; Area: may not cross the river; one cell in eight (by its move) | 2 | 2 / 3 |
| War Engine | Leaps two squares orthogonally | One cell in four (by its move) | 3 | — / — |
| Minister | One square orthogonally | — | 4 | — / — |
| Courtier | One square diagonally | One colour (by its move) | 2 | — / — |

### Specialty / Artillery

| Piece | Moves | Captures | Restrictions | Value | Lifts |
|---|---|---|---|---|---|
| Grasshopper | Along any line, over the first piece met, to the cell just beyond | Same | Move: must hop, landing just beyond | 14 | 3 / — |
| Cannon (Xiangqi) | Any distance orthogonally | By hopping one piece, at any distance beyond | — | 13 | — / — |
| Cannon (Janggi) | Over exactly one piece, to any cell beyond, orthogonally | Same | Move: must hop exactly one piece, never a Cannon of either kind | 12 | 3 / — |

## Review notes

### Settled (2026-10-25)

- The Silver and Gold Generals are **Leaders** (the sheet had them as Infantry).
- The **Dragon King** moves as in Shogi: any distance orthogonally, or one square diagonally.
- The **Archbishop** moves any distance diagonally, or leaps 2+1 (its description had been the
  Amazon's).
- The Met, which the sheet had called the **Private**, moves exactly as the **Courtier** does: the
  Makruk Pawn promotes to the Courtier, and the Private is gone.
- The Xiangqi General's speed of 7 is meant: it prices in the flying-general capture.
- A colour binding that a piece's move imposes cannot be bought off: the Bishop's, Courtier's and
  Alfil's lifts are gone. So is the War Engine's, for the same reason.
- The **Camel**'s and **Threeleaper**'s bindings are listed, without a lift.
- A bound piece's devaluation is set by the share of the board it can reach ([Binding](#binding)).
- Direction counts are what the value rests on, and are corrected where the move was mistold: the
  **Silver General** and the **Noble** (the Makruk Khon) step diagonally or straight forward (5;
  the Noble's bonus was 8, its value is now 5); the **Janggi Soldier** steps forward or sideways,
  and diagonally forward along palace lines (5, not 6; value unchanged).
- A Horse whose restriction is lifted (10 + 2) costs a point more than a Knight bought outright
  (11): meant, as the price of not buying the Knight.
- **Cannon (Janggi)**: it may not jump over a Cannon of either kind. *Proposed:* unlike in Janggi,
  it may capture one.

### Still open

Together, **Q20** of the [specification](specification.md#9-open-questions).

1. **Minister** and **Courtier**: classed as *Cavalry / Leaper*, though they step. The class decides
   which die moves them.
2. Three restrictions have no lift cost: the Knightrider's, the Janggi Soldier's and the Xiangqi
   Soldier's movement restrictions. Are they part of what the piece *is*, and not for sale?
3. The Zebra (3+2) changes colour every leap, so for it "one colour" is a real rule, which a lift
   removes. Is that the intent?
4. By the formula the Knight (11) is worth what the Rook is, and twice the Xiangqi Soldier; in
   chess a Rook is usually reckoned at nearly twice a Knight. Raised only in case it is not meant.

### Names the program keeps apart

The grand **Horse** (blockable) and **Elephants** move differently from the classic Horse (a
Knight) and Elephant (a Rook), and there are four Pawns. The program gives each its own key
(`western-pawn`, `xiangqi-elephant`…), whatever the page calls it.
