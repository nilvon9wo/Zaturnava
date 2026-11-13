# The grand game — Pieces

The pieces of the [grand game](specification.md), as listed in the designer's spreadsheet,
`docs/Other Potential Pieces.xlsx`, grouped by class (within a class, not necessarily most valuable
first). The spreadsheet is the
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
  capture is worth 7. The Western Pawn's start-row powers are worth 3 (*Proposed*, 2026-11-05; 1
  before *en passant* was widened to take any enemy piece moving against the pawn, past it): 1 for the
  double first step, 2 for an *en passant* that may now, now and then, take a heavy piece. To be
  settled by playtesting (**Q14**).
- The sheet counts reach on 8 × 8; a game on another board adjusts it, as
  [Prices for each game](#prices-for-each-game) says.

### Limitation devaluation

A restriction is devalued only for what reach does not already count (*Stated* 2026-11-12, after the
Xiangqi Elephant was found priced as the Alfil though it is the weaker piece; *Proposed*: the points):

- a [binding](#binding): −1 for half the board, −2 for a quarter or less;
- a palace: −2;
- a leap that a piece on the way can block: −1;
- a move or area restriction that takes something away: −1 (a Western or Makruk Pawn cannot take
  straight ahead, so a piece there stops it dead; a Soldier may not go back).

A restriction that only restates how the piece moves costs nothing, as for the Rook or the
Knightrider: a slider blocked by a piece in its path (the Lance), a piece that takes what is straight
ahead (the Shogi Pawn), a hopper that must hop (the Grasshopper, whose reach is already counted with
pieces to hop). Each had −1 before, now gone.

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
| Lance (Shogi) | Gold General, 12 over 5 rows | 2 | 56 |
| Pawn (Shogi) | Gold General, 15 over 4 rows | 4 | 55 |
| Pawn (Western) | Queen, 22 over 6 rows | 4 | 9 |
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
so the premium shrinks: the Lance is worth 56 (50 + reach 4 + premium 2), the Shogi Pawn 55
(50 + 1 + 4). The Silver General is a leader already. The value is what capturing the piece
scores too.

### Prices for each game

The sheet prices every piece on an 8 × 8 board. A game may be set on a much larger board, where a
promotion is further away and a slider reaches further, or a smaller one. *Stated* (2026-11-06): the
program prices each game's pieces on that game's own board, if that is not too complicated; and from the
board's actual dimensions, adjusting the base price, never from values set for each size, as the quick
games' sizes are only conveniences. *Proposed*: by the same rules, on that game's board and kingdoms, the
prices shown in the draft; the sheet stays the reference, on 8 × 8.

- Reach is counted from the board's centre (d4 on 8 × 8), with a piece on every cell next to a hopper
  if that reaches more.
- A promotion's distance is counted from the kingdom's front row, the furthest forward a piece may
  start, to the zone: placing a piece well forward cannot buy its premium cheap. A Western piece's
  zone is the last row, a Shogi piece's the far three, a Makruk Pawn's the enemy kingdom ahead; the
  far edge is the last row.

*Done*: a piece's price on a game's board is its sheet value, plus the cells it reaches there less
those it reaches on 8 × 8, plus its premium there less its sheet premium (kept, as the sheet has it, in
`Values/grand-legions-premiums.json`). On the medium game's 8 × 8 board, kingdoms 2 deep, the prices are
the sheet's, but the Lance's: 57, not 56, as the sheet counts its promotion from row 1 (5 rows, premium 2) and
the board from its kingdom's front row (4 rows, premium 3). *Proposed*: kept, the board's count being the one a
player can use. *Proposed*: a price is never below 1, as a piece that reaches nothing from the centre of a
small board (the Giraffe on 6 × 6) would otherwise be free, and an army could fill its kingdom with it.
The price is what the piece costs in a quick army and what taking it scores.

| Piece | Small (6 × 6) | Medium (8 × 8) | Large (12 × 12) |
|---|---|---|---|
| Rook | 10 | 14 | 22 |
| King (Western) | 70 | 70 | 70 |
| Pawn (Western) | 12 | 9 | 7 |
| Giraffe | 1 | 4 | 8 |

### Binding

Some pieces can never leave part of the board, whatever is paid: a piece that moves only diagonally
keeps to one colour, half the board; the War Engine keeps to a quarter, the Alfil and the Xiangqi
Elephant to an eighth, the Threeleaper to a ninth. Whatever stands on the rest, a leader above all,
is safe from it. Such a piece's limitation devaluation is set by the share of the board it can
reach, apart from any other restriction it has:

| Reaches | Devaluation | Pieces |
|---|---|---|
| a half | −1 | Bishop, Camel, Courtier, King (Checkers), Man |
| a quarter or less | −2 | War Engine, Alfil, Threeleaper |

The Xiangqi Elephant has −3 in all: −2 for its binding, as the Alfil, and −1 for its blocked leg.
The river binds it further (it keeps to its own half, where it reaches seven cells), but a quarter or
less already costs the most a binding costs, so the river adds nothing. It was −2, priced as the
Alfil, though it is the weaker piece (2026-11-12). The
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
| King (Tafl) | Any distance orthogonally; takes only by custody | — | [Custody](#jumping-and-custody) | 71 | — / — |
| King (Western) | One square in any direction | — | Castling | 70 | — / — |
| General (Xiangqi) | One square orthogonally | Area: its 3 × 3 palace | Flying-general capture | 69 | — / 5 |
| General (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Leader face-off; palace lines | 68 | — / 5 |
| Gold General (Shogi) | One square orthogonally, or diagonally forward | — | — | 66 | — / — |
| Silver General (Shogi) | One square diagonally, or straight forward | — | → Gold General in the far three ranks | 65 | — / — |
| King (Checkers) | One square diagonally; takes only by jumping | One colour (by its move) | [Jump chains](#jumping-and-custody) | 64 | — / — |

### Infantry

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Pawn (Shogi) | One square forward | Move: blocked straight ahead | → Gold General in the far three ranks | 55 | 2 / — |
| Pawn (Western) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | First move two squares; *en passant*, of any enemy piece moving two squares or more against it, past it, from two squares away; → Queen, Rook, Bishop or Knight on the last rank | 9 | 2 / — |
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
| Dragon Bishop (custom) | Any distance diagonally, or leaps 2+1 | — | — | 21 | — / — |
| Dragon Horse (Shogi) | Any distance diagonally, or one square orthogonally | — | — | 17 | — / — |
| Rook (Western) | Any distance orthogonally | — | — | 14 | — / — |
| Bishop (Western) | Any distance diagonally | One colour (by its move) | — | 12 | — / — |
| Swede (Tafl) | Any distance orthogonally; takes only by custody | — | [Custody](#jumping-and-custody) | 11 | — / — |

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
| Elephant (Xiangqi) | Two squares diagonally | Move: the square between must be empty; Area: may not cross the river; one cell in eight (by its move) | 1 | 2 / 3 |
| War Engine | Leaps two squares orthogonally | One cell in four (by its move) | 2 | — / — |

### Specialty / Artillery

| Piece | Moves | Captures | Restrictions | Value | Lifts |
|---|---|---|---|---|---|
| Man (Checkers) | One square diagonally forward | By [jumping](#jumping-and-custody), diagonally forward; → King (Checkers) on the last rank | One colour (by its move) | 54 | — / — |
| Cannon (Xiangqi) | Any distance orthogonally | By hopping one piece, at any distance beyond | — | 10 | — / — |
| Cannon (Janggi) | Over exactly one piece, to any cell beyond, orthogonally | Same | Move: must hop exactly one piece; may neither hop nor capture a Cannon of either kind | 8 | 3 / — |
| Grasshopper | Along any line, over the first piece met, to the cell just beyond | Same | Move: must hop, landing just beyond | 8 | 3 / — |

### Jumping and custody

Four pieces, rows 41 to 44 of the sheet (added by the designer, 2026-11-13; the columns they left open filled in
by the program's rules, *Proposed*), come from Checkers and Tafl and take in ways no other piece does.

**Jumping** (the Checkers King and the Man). The piece takes a piece next to it, diagonally, by jumping over it
to the empty cell just beyond; from there it may jump again, as often as it can. *Proposed*:

- each jump of the chain is a move of its own, so the piece may stop after any of them; it need not take all
  it could, nor take at all;
- of the chains that end on the same cell, the one that takes the most is played (the first found, among
  those that take as many); a chain may not end where it began;
- the Man jumps only diagonally forward, as it steps; the King, either way.

**Custody** (the Tafl King and the Swede). The piece takes nothing where it lands. Moving next to an enemy
piece, orthogonally, with a piece its mover commands just beyond that one, it takes it; every piece so held
about it at once. *Proposed*: only the custodian's own move takes; a piece that moves between two others is
not taken, nor is one that another of the mover's pieces comes to stand beyond; an ally's piece is never taken.

Their values, by the sheet's columns:

| Piece | Leader | Reach | Premium | Powers | Limits | Value |
|---|---|---|---|---|---|---|
| King (Tafl) | 60 | 14 | 0 | +1 | −4 | 71 |
| King (Checkers) | 60 | 4 | 0 | +2 | −2 | 64 |
| Man | 50 | 2 | 2 | +2 | −2 | 54 |
| Swede | 0 | 14 | 0 | +1 | −4 | 11 |

- Reach counts the cells a piece moves to or takes on. A jump takes on the cell next to the jumper, which its
  step reaches already, so a jump adds no reach.
- Powers: taking several pieces in one move. A jump chain can go on and on (+2). Custody takes at most the
  three pieces about the cell (+1).
- Limits: a Checkers piece keeps to one colour (−1, [Binding](#binding)) and takes only by jumping, which
  needs the cell beyond empty (−1). A custodian takes only by custody, which needs a second piece. That costs
  what taking only by hopping costs the Xiangqi Cannon (−4).
- The Man may promote into the Checkers King, a leader, so it starts from the
  [leader promotion bonus](#leader-promotion-bonus) (50). Its premium is the 12 that promotion would gain
  (64 less 52), over 7 rows: 2.
- The sheet also says the Tafl King "can permanently escape the game". **Escape**, *Stated* (2026-11-13): on its
  own army's far corner (the right-hand corner of the far row, as the army faces; z26 for Black on the largest board),
  and only there, the Tafl King may leave the board, by a deliberate move or on a die that moves it. Off the board
  no one can take it. It comes back only as a prisoner escapes, on doubles that move it, to its home.
  *Proposed*: while it is away its army plays on as one whose leader was taken, and a fled King, like a prisoner,
  holds no ground: if it was its team's last piece on the board, the game ends as when a team is left alone.
  The escape is unpriced: its worth is an [open question](../open-questions.md).

### Editions

A game stored before these pieces came must replay as it was played, and an army is dealt or offered from
the pieces there are. So every piece carries the edition it came in (0 for the catalogue as it was, 1 for
these four), and every game records the edition it was started in. A game deals and offers only the pieces of
its edition or before; a game started before editions were recorded is of edition 0. New games are of the
latest edition.

## Review notes

### Settled (2026-10-25)

- The Silver and Gold Generals are **Leaders** (the sheet had them as Infantry).
- The **Dragon King** moves as in Shogi: any distance orthogonally, or one square diagonally.
- The **Archbishop** moves any distance diagonally, or leaps 2+1 (its description had been the
  Amazon's). (Since gone: it was the Dragon Bishop, which stays; see the review of 2026-11-06.)
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

- **Intended (Stated 2026-11-06): the Shogi Pawn costs more than the Western Pawn.** It is the
  weaker piece on its own, but it may be promoted into a leader (the Gold General), and the
  [leader promotion bonus](#leader-promotion-bonus) prices it in line with the other pieces that
  may become leaders. A discrepancy between a piece's price and its strength is not wrong in itself
  (it may be a good or a bad investment), but it should be intended, not an accident of the formula.

### Review of the catalogue (2026-11-06)

The catalogue gone through for other discrepancies between a piece's price and its strength (backlog N30).
What was found, and what is made of it:

- **Weaker pieces dearer for their promotions.** The Western Pawn (9) costs more than the Noble (7) or the
  Minister (6), which move better; the Lance (56) more than the Amazon (37), the strongest piece that is not a
  leader. Both are the Shogi Pawn's case: the price is mostly the option to promote (into a Queen; into a Gold
  General, a leader). *Proposed*: intended, by the same reasoning as the Shogi Pawn's.
- **What taking such a piece scores.** A piece's value is also what taking it scores, so taking a Lance (56)
  or a Shogi Pawn (55) scores more than taking a Queen (27). *Proposed*: intended, as it follows from two
  Stated rules (price is value; value is the score); a player who buys the option puts it at risk.
- **The Dragon Bishop was the Archbishop.** Both moved any distance diagonally or leapt 2+1, and both were
  worth 21, perhaps one piece under two games' names. *Stated*: the Dragon Bishop stays, in keeping with the
  other Dragons, and the Archbishop is gone, from the catalogue and the sheet.
- **Values are counted on 8 × 8, but the quick games are 6 × 6, 8 × 8 and 12 × 12.** On the small board a
  slider reaches less and a long leaper far less: the Giraffe (4+1) cannot move at all from the four centre
  cells, yet costs what it does on 8 × 8; on the large board sliders and leapers reach more. The Makruk Pawn's
  premium rests on four rows to its sixth rank, but it now promotes on entering an enemy kingdom: one row
  ahead on the small board; on the medium and large boards three and four rows ahead on the files that lead
  into one, and on the others only by taking aside. *Done*:
  [Prices for each game](#prices-for-each-game) settles it, each game's pieces priced on its own board.
- **Hoppers are counted at their best.** A hopper's reach is counted with a piece on every cell next to it, the
  most it could reach; on an emptying board it may have nothing to hop, so the Grasshopper (8 since 2026-11-12; 7
  then) and the Janggi Cannon (8) may be dear beside the Knight (8). *Proposed*: kept as it is, for playtesting to settle.
- Nothing else stands out: leaders differ by their reach and powers (the Dragon King 78 above the Fairy
  General 76 above the Western King 70), compound pieces are about the sum of their parts, and the bound
  pieces lose a point or two, as [Binding](#binding) sets.

### Names the program keeps apart

The grand **Horse** (blockable) and **Elephants** move differently from the Chaturaji 2027 Horse (a
Knight) and Elephant (a Rook), and there are four Pawns and four Kings (`checkers-king`, `tafl-king`…). The program gives each its own key
(`western-pawn`, `xiangqi-elephant`…), whatever the page calls it: `GrandLegionsPieces`, in
`Zaturnava.Games.GrandLegions`, holds the whole catalogue, each piece with its class, its tradition
(Western, Xiangqi, Janggi, Shogi, Makruk, Checkers, Tafl, or fairy), its edition, its value, its moves, its restrictions and their
lifts, and its promotions.
