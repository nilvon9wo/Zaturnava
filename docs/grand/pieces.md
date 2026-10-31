# The grand game — Pieces

The pieces of the [grand game](specification.md), as listed in the designer's spreadsheet,
`docs/Other Potential Pieces.xlsx`, most valuable first within each class. The spreadsheet is the
source; this page is a readable copy of it, and the two are changed together.

## Value

A piece's value is the sum of the sheet's columns (`=O+P+Q+R+S`):

| Part | What it counts |
|---|---|
| Leader bonus | 60 for a leader; 50 for a piece that may promote into one ([below](#leader-promotion-bonus)); else 0 |
| Reach | the cells it could move to or capture on from d4, on an empty 8 × 8 board |
| Promotion premium | what the chance of promoting is worth ([below](#promotion-premium)) |
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

### Promotion premium

A piece that may promote is bought, in part, for what it may become: its price carries a premium,
as an option's does. The premium is what promotion would gain it (the promoted piece's value, less
its own without the premium) over the distance it has to cross and survive, counted on 8 × 8 from
row 2 for Infantry and row 1 for the rest. At a premium factor of 1 the bet breaks even if one in
"distance" such pieces gets through: cheap enough to be worth the risk, not so cheap as to be no
risk. The value is also what the piece scores when captured, so a piece taken before it promotes
hands its captor the premium too.

Every Infantry piece may also promote at the far edge into any captured piece. Which pieces will
have been captured is not known when the army is bought, so that option is priced as a promotion
into a stand-in, the Rook, over 6 rows; an Infantry piece pays the higher of its own premium and
this one, as it can take only one of them. A piece confined to its palace cannot reach the far edge,
and has no such premium.

| Piece | Promotes to | Premium | Value |
|---|---|---|---|
| Lance (Shogi) | Gold General, 13 over 5 rows | 3 | 56 |
| Pawn (Shogi) | Gold General, 16 over 4 rows | 4 | 54 |
| Pawn (Western) | Queen, 24 over 6 rows | 4 | 7 |
| Silver General (Shogi) | Gold General, 1 over 5 rows | 0 | 65 |
| Pawn (Makruk) | Courtier, 3 over 4 rows; the far edge pays more | 2 | 4 |
| Other Infantry, not confined | the far edge (Rook, 14), over 6 rows | 2 | +2 |

The sheet keeps the four settings apart, to be tuned by playtesting: `PremiumFactor` (1),
`FarEdgeTarget` (the Rook), `FarEdgeDistance` (6) and `LeaderPromotionBonus` (50).

### Leader promotion bonus

A piece that is not a leader but may promote into one starts from a leader bonus of **50**, as a
leader starts from 60 (Stated 2026-10-31); its reach, premium, powers and limitations count on top,
as for any piece, so two such pieces need not be worth the same. The premium alone undervalued
them: it assumes the piece has the whole distance to cross, but how far that is depends on how its
owner arranges the kingdom, and a Lance with an open file may slide into the far three ranks on its
first move and promote into a Gold General, a leader, at once. With the bonus, promotion gains less,
so the premium shrinks: the Lance is worth 56 (50 + reach 4 + premium 3 − 1), the Shogi Pawn 54
(50 + 1 + 4 − 1). The Silver General is a leader already. The value is what capturing the piece
scores too.

### Prices for each game (proposed)

The sheet prices every piece on an 8 × 8 board. A game may be set on a much larger board, where a
promotion is further away and a slider reaches further, or a smaller one. **Proposed**, for later:
the program prices each game's pieces by the same rules, on that game's board and kingdoms, and
shows the prices in the draft; the sheet stays the reference, on 8 × 8.

- Reach is counted from the board's centre.
- A promotion's distance is counted from the kingdom's front row, the furthest forward a piece may
  start, to the zone: placing a piece well forward cannot buy its premium cheap.

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

The dice name a class, not a piece ([specification §6.1](specification.md#61-the-chaturaji-2027-rules)).

### Leaders

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Dragon King (Shogi) | Any distance orthogonally, or one square diagonally | — | — | 78 | — / — |
| Fairy General (custom) | One square in any direction, or leaps 2+1 | — | — | 76 | — / — |
| King (Western) | One square in any direction | — | Castling | 70 | — / — |
| General (Xiangqi) | One square orthogonally | Area: its 3 × 3 palace | Flying-general capture | 69 | — / 5 |
| General (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Leader face-off; palace lines | 68 | — / 5 |
| Gold General (Shogi) | One square orthogonally, or diagonally forward | — | — | 66 | — / — |
| Silver General (Shogi) | One square diagonally, or straight forward | — | → Gold General in the far three ranks | 65 | — / — |

### Infantry

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Pawn (Shogi) | One square forward | Move: blocked straight ahead | → Gold General in the far three ranks | 54 | 2 / — |
| Pawn (Western) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | First move two squares; *en passant*; → Queen, Rook, Bishop or Knight on the last rank | 7 | 2 / — |
| Guard (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace lines | 7 | — / 5 |
| Noble (Makruk) | One square diagonally, or straight forward | — | — | 7 | — / — |
| Soldier (Janggi) | One square forward or sideways (diagonally forward along palace lines) | Move: may not go back; Area: palace diagonals only | Palace lines | 7 | 2 / 2 |
| Minister | One square orthogonally | — | — | 6 | — / — |
| Courtier | One square diagonally | One colour (by its move) | — | 5 | — / — |
| Pawn (Makruk) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | → Courtier on the sixth rank | 4 | 2 / — |
| Soldier (Xiangqi) | One square forward, or sideways | Move: no sideways before crossing the river, never back | — | 4 | 2 / 3 |
| Advisor (Xiangqi) | One square diagonally | Area: its 3 × 3 palace | — | 2 | — / 5 |

Any Infantry piece that reaches the far edge may also be promoted into any captured piece, as in
Chaturaji 2027.

### Heavy / Slider

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Lance (Shogi) | Any distance straight forward | Move: blocked by pieces in its path | → Gold General in the far three ranks | 56 | 2 / — |
| Amazon | Any distance in any direction, or leaps 2+1 | — | — | 37 | — / — |
| Queen (Western) | Any distance in any direction | — | — | 27 | — / — |
| Chancellor | Any distance orthogonally, or leaps 2+1 | — | — | 22 | — / — |
| Archbishop | Any distance diagonally, or leaps 2+1 | — | — | 21 | — / — |
| Dragon Bishop (custom) | Any distance diagonally, or leaps 2+1 | — | — | 21 | — / — |
| Dragon Horse (Shogi) | Any distance diagonally, or one square orthogonally | — | — | 17 | — / — |
| Rook (Western) | Any distance orthogonally | — | — | 14 | — / — |
| Bishop (Western) | Any distance diagonally | One colour (by its move) | — | 12 | — / — |

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
- A promotion is priced as an option, and every Infantry piece holds one on the far edge
  ([Promotion premium](#promotion-premium)). A Shogi Pawn or Lance promotes into a Gold General,
  which is a leader: high risk, high reward, priced to match.

### Still open

Together, **Q20** of the [specification](specification.md#9-open-questions).

- **For playtesting (Stated 2026-10-29): the Pawns' promotion premiums may be out of balance.** The
  Shogi Pawn was priced at 17 against the Western Pawn's 7, yet it is the weaker piece; the
  [leader promotion bonus](#leader-promotion-bonus) now prices it at 54, widening the gap. It
  captures on one square to the Western Pawn's two. It has no powers to the Western Pawn's two
  (the double first step and *en passant*). It promotes into one piece, if a powerful one (the
  Gold General, a leader), to the Western Pawn's four, the Queen among them. And it is likelier to
  promote, needing 4 rows to the Western Pawn's 6. No change for now: investigate and playtest it
  once work on the grand game begins (spec Q14).

### Names the program keeps apart

The grand **Horse** (blockable) and **Elephants** move differently from the Chaturaji 2027 Horse (a
Knight) and Elephant (a Rook), and there are four Pawns. The program gives each its own key
(`western-pawn`, `xiangqi-elephant`…), whatever the page calls it: `GrandLegionsPieces`, in
`Zaturnava.Games.GrandLegions`, holds the whole catalogue, each piece with its class, its tradition
(Western, Xiangqi, Janggi, Shogi, Makruk, or fairy), its value, its moves, its restrictions and their
lifts, and its promotions.
