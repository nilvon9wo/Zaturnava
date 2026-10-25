# The grand game — Pieces

The pieces of the [grand game](specification.md), as listed in the designer's spreadsheet,
`docs/Other Potential Pieces.xlsx`, most valuable first within each class. The spreadsheet is the
source; this page is a readable copy of it, and the two are changed together.

## Value

A piece's value is the sum of the sheet's bonus columns (`=O+P+Q+R+S`):

| Part | What it counts |
|---|---|
| Leader bonus | 60 for a leader, else 0 |
| Reach | the cells it could move to or capture on from d4, on an empty 8 × 8 board |
| Promotion bonus | what its promotion is worth (Western Pawn 5, Shogi Pawn 4…) |
| Special-power bonus | what its powers are worth, where reach does not count them |
| Limitation devaluation | what its restrictions take off |

Reach replaced a direction bonus and a speed bonus (counted twice), which overvalued long leaps and
hops, and undervalued a piece that moves along many lines at once. Reach follows the values chess
players use (Queen 27, Rook 14, Bishop 13, Knight 8, King 8, against 9, 5, 3, 3) and a compound
piece is worth about its parts (Chancellor 22 = Rook 14 + Knight 8).

- Reach counts the moves a piece's rules allow, wherever they are allowed: a piece with palace
  lines counts them, and an infantry piece forward as up the board.
- A hopper needs a piece to hop, and on an empty board has none: its reach is counted with a piece
  on every cell next to it (the Grasshopper 8, the Janggi Cannon 10). The Xiangqi Cannon moves as a
  Rook does (14), but takes only by hopping (−4).
- A power that reach does not see is a special-power bonus: the Xiangqi General's flying-general
  capture is worth 7.
- Reach is counted on 8 × 8 even when the board is larger, so that it stays bounded.

### Binding

Some pieces can never leave part of the board, whatever is paid: a piece that moves only diagonally
keeps to one colour, half the board; the War Engine keeps to a quarter, the Alfil and the Xiangqi
Elephant to an eighth, the Threeleaper to a ninth. Whatever stands on the rest, a leader above all,
is safe from it. Such a piece's limitation devaluation is set by the share of the board it can
reach, apart from any other restriction it has:

| Reaches | Devaluation | Pieces |
|---|---|---|
| a half | −1 | Bishop, Camel, Courtier |
| a quarter or less | −2 | War Engine, Alfil, Threeleaper |

The Xiangqi Elephant has −2 in all: −1 for its blocked leg, and −1 for the river and its binding
together, which overlap (the river keeps it in its own half, where it reaches seven cells). The
Advisor has −2 for its palace, which binds it more than its colour does. A bound piece is not
worthless: two Bishops on opposite colours cover the board between them, which is why the
devaluation is a point or two, and not a fraction of the value.

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
| Dragon King (Shogi) | Any distance orthogonally, or one square diagonally | — | — | 78 | — / — |
| Fairy General (custom) | One square in any direction, or leaps 2+1 | — | — | 76 | — / — |
| King (Western) | One square in any direction | — | Castling | 70 | — / — |
| General (Xiangqi) | One square orthogonally | Area: its 3 × 3 palace | Flying-general capture | 69 | — / 5 |
| Silver General (Shogi) | One square diagonally, or straight forward | — | → Gold General in the far three ranks | 68 | — / — |
| General (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace face-off; palace lines | 68 | — / 5 |
| Gold General (Shogi) | One square orthogonally, or diagonally forward | — | — | 66 | — / — |

### Infantry

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Pawn (Western) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | First move two squares; *en passant*; → Queen, Rook, Bishop or Knight on the last rank | 8 | 2 / — |
| Guard (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace lines | 7 | — / 5 |
| Noble (Makruk) | One square diagonally, or straight forward | — | — | 5 | — / — |
| Soldier (Janggi) | One square forward or sideways (diagonally forward along palace lines) | Move: may not go back; Area: palace diagonals only | Palace lines | 5 | 2 / 2 |
| Minister | One square orthogonally | — | — | 4 | — / — |
| Pawn (Shogi) | One square forward | Move: blocked straight ahead | → Gold General in the far three ranks | 4 | 2 / — |
| Pawn (Makruk) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | → Courtier on the sixth rank | 3 | 2 / — |
| Courtier | One square diagonally | One colour (by its move) | — | 3 | — / — |
| Advisor (Xiangqi) | One square diagonally | Area: its 3 × 3 palace | — | 2 | — / 5 |
| Soldier (Xiangqi) | One square forward, or sideways | Move: no sideways before crossing the river, never back | — | 2 | 2 / 3 |

Any Infantry piece that reaches the far edge may also be promoted into any captured piece, as in
the classic game.

### Heavy / Slider

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Amazon | Any distance in any direction, or leaps 2+1 | — | — | 37 | — / — |
| Queen (Western) | Any distance in any direction | — | — | 27 | — / — |
| Chancellor | Any distance orthogonally, or leaps 2+1 | — | — | 22 | — / — |
| Archbishop | Any distance diagonally, or leaps 2+1 | — | — | 21 | — / — |
| Dragon Bishop (custom) | Any distance diagonally, or leaps 2+1 | — | — | 21 | — / — |
| Dragon Horse (Shogi) | Any distance diagonally, or one square orthogonally | — | — | 17 | — / — |
| Rook (Western) | Any distance orthogonally | — | — | 14 | — / — |
| Bishop (Western) | Any distance diagonally | One colour (by its move) | — | 12 | — / — |
| Lance (Shogi) | Any distance straight forward | Move: blocked by pieces in its path | → Gold General in the far three ranks | 6 | 2 / — |

### Cavalry / Leaper

| Piece | Moves | Restrictions | Value | Lifts |
|---|---|---|---|---|
| Knightrider | Repeats 2+1 leaps in one direction | Move: each landing cell short of the last must be empty | 12 | 3 / — |
| Zebra | Leaps 3+2 | — | 8 | — / — |
| Knight | Leaps 2+1 | — | 8 | — / — |
| Camel | Leaps 3+1 | One colour (by its move) | 7 | — / — |
| Horse | Leaps 2+1 | Move: the first square of the two-square leg must be empty | 7 | 2 / — |
| Elephant (Janggi) | One orthogonal step, then two diagonal steps outward | Move: each cell on the way must be empty | 7 | 2 / — |
| Giraffe | Leaps 4+1 | — | 4 | — / — |
| Elephant (Alfil) | Leaps 2+2 diagonally | One cell in eight (by its move) | 2 | — / — |
| Threeleaper | Leaps three squares orthogonally | One cell in nine (by its move) | 2 | — / — |
| Elephant (Xiangqi) | Two squares diagonally | Move: the square between must be empty; Area: may not cross the river; one cell in eight (by its move) | 2 | 2 / 3 |
| War Engine | Leaps two squares orthogonally | One cell in four (by its move) | 2 | — / — |

### Specialty / Artillery

| Piece | Moves | Captures | Restrictions | Value | Lifts |
|---|---|---|---|---|---|
| Cannon (Xiangqi) | Any distance orthogonally | By hopping one piece, at any distance beyond | — | 10 | — / — |
| Cannon (Janggi) | Over exactly one piece, to any cell beyond, orthogonally | Same | Move: must hop exactly one piece; may neither hop nor capture a Cannon of either kind | 8 | 3 / — |
| Grasshopper | Along any line, over the first piece met, to the cell just beyond | Same | Move: must hop, landing just beyond | 7 | 3 / — |

## Review notes

### Settled (2026-10-25)

- The Silver and Gold Generals are **Leaders** (the sheet had them as Infantry).
- The **Dragon King** moves as in Shogi: any distance orthogonally, or one square diagonally.
- The **Archbishop** moves any distance diagonally, or leaps 2+1 (its description had been the
  Amazon's).
- The Met, which the sheet had called the **Private**, moves exactly as the **Courtier** does: the
  Makruk Pawn promotes to the Courtier, and the Private is gone.
- The Xiangqi General's flying-general capture is priced in: once as a speed of 7, now as a
  special-power bonus of 7.
- A colour binding that a piece's move imposes cannot be bought off: the Bishop's, Courtier's and
  Alfil's lifts are gone. So is the War Engine's, for the same reason.
- The **Camel**'s and **Threeleaper**'s bindings are listed, without a lift.
- A bound piece's devaluation is set by the share of the board it can reach ([Binding](#binding)).
- Direction counts are what the value rests on, and are corrected where the move was mistold: the
  **Silver General** and the **Noble** (the Makruk Khon) step diagonally or straight forward (5;
  the Noble's bonus was 8, its value is now 5); the **Janggi Soldier** steps forward or sideways,
  and diagonally forward along palace lines (5, not 6; value unchanged).
- A Horse whose restriction is lifted (7 + 2) costs a point more than a Knight bought outright
  (8): meant, as the price of not buying the Knight.
- Lifts for the three movement restrictions that had none: the **Knightrider** 3 (it may leap on
  past an occupied landing cell), each **Soldier** 2 (it may step back). Lift prices stay as they
  were set, beside the new values.
- **Cannon (Janggi)**: as in Janggi, it may neither jump over nor capture a Cannon, of either kind;
  its devaluation is −2 for it. Every Cannon is the safer for it.
- The **Minister** and **Courtier** are **Infantry** (the sheet had them as Cavalry / Leaper).
- The **Zebra** has no colour restriction: its leap changes colour every time.
- Value counts **reach** in place of a direction bonus and a speed bonus ([Value](#value)).

### Still open

Together, **Q20** of the [specification](specification.md#9-open-questions).

1. **Pricing a promotion** (*proposed*). A promotion is an option: worth what the piece would gain,
   times the chance of getting there. The designer's first idea, the promoted value over the
   distance to cross, is that, with one over the distance standing for the chance. Taking the gain
   (the promoted value less the piece's own value without the bonus) and counting distance on
   8 × 8 to the first row where its condition holds, from row 2 for Infantry (the furthest it may
   start and keep its start-row powers) and from row 1 for the rest:

   | Piece | Promotes to | Gain | Distance | Bonus now | Bonus proposed | Value |
   |---|---|---|---|---|---|---|
   | Pawn (Western) | Queen (27) | 24 | 6 | 5 | 4 | 7 |
   | Pawn (Shogi) | Gold General (66) | 66 | 4 | 4 | 17 | 17 |
   | Lance (Shogi) | Gold General (66) | 63 | 5 | 3 | 13 | 16 |
   | Silver General (Shogi) | Gold General (66) | 1 | 5 | 3 | 0 | 65 |
   | Pawn (Makruk) | Courtier (3) | 1 | 4 | 1 | 0 | 2 |

   The Gold General is now a **Leader**, so a Shogi Pawn or a Lance that reaches the far three ranks
   becomes a leader worth 66, free: by this pricing, worth more than a Rook from the start. Is a
   promoted Gold General meant to be a leader (with its leader bonus, and all a leader may do)?
   The far-edge promotion into any captured piece is open to every Infantry piece alike, so it is
   left out of every piece's price.

### Names the program keeps apart

The grand **Horse** (blockable) and **Elephants** move differently from the classic Horse (a
Knight) and Elephant (a Rook), and there are four Pawns. The program gives each its own key
(`western-pawn`, `xiangqi-elephant`…), whatever the page calls it.
