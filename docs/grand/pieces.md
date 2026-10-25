# The grand game — Pieces

The pieces of the [grand game](specification.md), as listed in `docs/Other Potential Pieces.xlsx`
on 2026-10-25, most valuable first. Where this page and the sheet disagree, the sheet is the source
until the designer settles the [review notes](#review-notes).

## Value

A piece's value is the sum of the sheet's bonus columns; the sheet enters each by hand (it has no
formulas):

| Part | As entered |
|---|---|
| Leader bonus | 60 for a leader, else 0 |
| Direction bonus | usually the number of directions it may move in (1 direction: 0) |
| Speed bonus | its greatest distance; 0 for a piece that moves one square |
| Promotion bonus | what its promotion is worth (Western Pawn 5, Shogi Pawn 4…) |
| Special-power bonus | what its powers are worth (1 to 3) |
| Limitation devaluation | −1 or −2 for each kind of restriction |

The same value is the draft cost, and, by the designer's notes, the cost of promoting into the
piece during play.

## Catalogue

"Lifts" gives the cost of lifting the movement restriction and the area restriction, in that
order; "—" means there is none to lift.

### Leaders

| Piece | Moves | Restrictions | Powers | Value | Lifts |
|---|---|---|---|---|---|
| Dragon King (Shogi) | Any distance orthogonally, or leaps 2+1 | — | — | 75 | — / — |
| General (Xiangqi) | One square orthogonally | Area: its 3 × 3 palace | Flying-general capture | 72 | — / 5 |
| Fairy General (custom) | One square in any direction, or leaps 2+1 | — | — | 71 | — / — |
| King (Western) | One square in any direction | — | Castling | 70 | — / — |
| General (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace face-off; palace lines | 68 | — / 5 |

### Infantry

| Piece | Moves | Restrictions | Powers / promotion | Value | Lifts |
|---|---|---|---|---|---|
| Silver General (Shogi) | One square diagonally | — | → Gold General in the far three ranks | 68 | — / — |
| Gold General (Shogi) | One square orthogonally, or diagonally forward | — | — | 66 | — / — |
| Pawn (Western) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | First move two squares; *en passant*; → Queen, Rook, Bishop or Knight on the last rank | 8 | 2 / — |
| Noble (Makruk) | One square diagonally forward | — | — | 8 | — / — |
| Guard (Janggi) | One square orthogonally, or diagonally along palace lines | Area: its 3 × 3 palace | Palace lines | 7 | — / 5 |
| Soldier (Janggi) | One square forward (diagonally forward along palace lines) | Move: may not go back; Area: palace diagonals only | Palace lines | 5 | — / 2 |
| Pawn (Shogi) | One square forward | Move: blocked straight ahead | → Gold General in the far three ranks | 3 | 2 / — |
| Pawn (Makruk) | One square forward; captures one square diagonally forward | Move: blocked straight ahead | → Met on the sixth rank | 3 | 2 / — |
| Private (Makruk) | One square diagonally forward | Area: one colour | — | 3 | — / 3 |
| Advisor (Xiangqi) | One square diagonally | Area: its 3 × 3 palace | — | 2 | — / 5 |
| Soldier (Xiangqi) | One square forward, or sideways | Move: no sideways before crossing the river, never back | — | 2 | — / 3 |

### Heavy / Slider

| Piece | Moves | Restrictions | Powers | Value | Lifts |
|---|---|---|---|---|---|
| Amazon | Any distance in any direction, or leaps 2+1 | — | — | 17 | — / — |
| Queen (Western) | Any distance in any direction | — | — | 15 | — / — |
| Archbishop | (sheet: as the Amazon; see notes) | — | — | 15 | — / — |
| Chancellor | Any distance orthogonally, or leaps 2+1 | — | — | 15 | — / — |
| Dragon Horse (Shogi) | Any distance diagonally, or one square orthogonally | — | — | 15 | — / — |
| Dragon Bishop (custom) | Any distance diagonally, or leaps 2+1 | — | — | 15 | — / — |
| Rook (Western) | Any distance orthogonally | — | — | 11 | — / — |
| Bishop (Western) | Any distance diagonally | Area: one colour | — | 9 | — / 3 |
| Lance (Shogi) | Any distance straight forward | Move: blocked by pieces in its path | → Gold General in the far three ranks | 9 | 2 / — |

### Cavalry / Leaper

| Piece | Moves | Restrictions | Value | Lifts |
|---|---|---|---|---|
| Knightrider | Repeats 2+1 leaps in one direction | Move: each landing cell short of the last must be empty | 16 | — / — |
| Giraffe | Leaps 4+1 | — | 13 | — / — |
| Zebra | Leaps 3+2 | Area: one colour | 12 | — / 3 |
| Knight | Leaps 2+1 | — | 11 | — / — |
| Camel | Leaps 3+1 | (none listed) | 11 | — / 3 |
| Horse | Leaps 2+1 | Move: the first square of the two-square leg must be empty | 10 | 2 / — |
| Elephant (Janggi) | One orthogonal step, then two diagonal steps outward | Move: each cell on the way must be empty | 10 | 2 / — |
| Elephant (Alfil) | Leaps 2+2 diagonally | Area: one colour | 7 | — / 3 |
| Elephant (Xiangqi) | Two squares diagonally | Move: the square between must be empty; Area: may not cross the river | 6 | 2 / 3 |
| Threeleaper | Leaps three squares orthogonally | (none listed) | 6 | — / 2 |
| War Engine | Leaps two squares orthogonally | Area: same-colour parity | 5 | — / 3 |
| Minister | One square orthogonally | — | 4 | — / — |
| Courtier | One square diagonally | Area: one colour | 3 | — / 3 |

### Specialty / Artillery

| Piece | Moves | Captures | Restrictions | Value | Lifts |
|---|---|---|---|---|---|
| Grasshopper | Along any line, over the first piece met, to the cell just beyond | Same | Move: must hop, landing just beyond | 14 | 3 / — |
| Cannon (Xiangqi) | Any distance orthogonally | By hopping one piece, at any distance beyond | — | 13 | — / — |
| Cannon (Janggi) | Over exactly one piece, to any cell beyond, orthogonally | Same | Move: must hop exactly one piece, never a Cannon | 12 | 3 / — |

## Review notes

Questions about the sheet itself, for the designer (together, **Q22** of the
[specification](specification.md#9-open-questions)). Each is a guess at a slip, not a change made.

### Descriptions that disagree with the other columns

1. **Dragon King (Shogi)**: described as Rook + Knight leap (which is the Chancellor), and listed as
   a *Leader*. In Shogi the Dragon King is a promoted Rook: Rook + one step diagonally. Which is
   meant, and is it meant to be a leader?
2. **General (Xiangqi)**: moves one square, but its speed is 7 (speed bonus 7). Is the 7 meant to
   price in the flying-general capture (which already has a power bonus of 3)? Otherwise its value
   would be 65.
3. **Silver General** and **Gold General**: classed as *Infantry* but given the leader bonus of 60
   (values 68 and 66). Without it they would be 8 and 6. And the Silver is described as moving only
   diagonally (4 directions) but given 5 directions: in Shogi it also steps straight forward.
4. **Archbishop**: described as the Amazon (orthogonal and diagonal slides, and the 2+1 leap), but
   the columns say diagonal and leap only, as the usual Archbishop (Bishop + Knight). The columns
   look right.
5. **Noble (Makruk)**: described as one square diagonally forward (2 directions), given 5
   directions and a direction bonus of 8. In Makruk the Khon moves as the Silver General
   (diagonally, or straight forward). With a bonus of 5 it would be worth 5.
6. **Private (Makruk)** and **Met**: the Makruk Pawn promotes to the Met, which is not in the list.
   The Private is described as one square diagonally forward, but given 4 directions, which is the
   Met's move (one square in any diagonal). Is the Private the Met?
7. **Soldier (Janggi)**: described as one square forward, given 6 directions and a direction bonus
   of 5. In Janggi a Soldier moves forward or sideways (and diagonally forward along palace lines).
8. **Minister** and **Courtier**: classed as *Cavalry / Leaper*, though they step.

### Restrictions that are geometry

9. A piece that only moves diagonally (Bishop, Courtier, Private, Alfil, Advisor) never leaves its
   colour, so "restricted to one colour" costs it nothing it had, and lifting it gives it nothing.
   Likewise the War Engine (2 orthogonally) only ever reaches a quarter of the board, and the
   Threeleaper a ninth, whatever is paid. Should these restrictions and their prices go, with the
   devaluation kept as the price of the geometry?
10. The **Zebra** (3+2) changes colour every leap, so for it "one colour" is a real rule, and
    lifting it does something. Is that the intent?
11. The **Camel** (3+1) is bound to one colour by geometry, and has a devaluation of −1 and an
    area-lift cost of 3, but no restriction listed. Likewise the **Threeleaper**.

### Prices

12. A Horse whose restriction is lifted is a Knight: 10 + 2 = 12, against 11 for a Knight bought
    outright. Should the lift cost 1, or is the premium intended?
13. Three restrictions have no lift cost: the Knightrider's, the Janggi Soldier's and the Xiangqi
    Soldier's movement restrictions. Are they part of what the piece *is*, and not for sale?
14. By the formula the Knight (11) is worth what the Rook is, and twice the Xiangqi Soldier; in
    chess a Rook is usually reckoned at nearly twice a Knight. The designer may want it so (the
    board is larger, and leapers do better in a crowd); the point is only raised.

### Names the program must keep apart

15. The grand **Horse** (blockable) and **Elephants** move differently from the classic Horse
    (a Knight) and Elephant (a Rook); and there are four Pawns. The program gives each its own key
    (`western-pawn`, `xiangqi-elephant`…), whatever the page calls it.
16. **Cannon (Janggi)** "cannot jump over another Cannon": either kind of Cannon, or only a Janggi
    one? In Janggi, a Cannon also may not capture a Cannon.
