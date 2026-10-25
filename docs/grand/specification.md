# The grand game — Specification (v0.1 draft)

The [classic game](../rules.md) is one small part of a larger design, which has no name yet; this
document calls it **the grand game** until it does. The classic game stays available, unchanged,
as a variant of its own.

Sources: the designer's notes of 2026-10-25 and the spreadsheet `docs/Other Potential Pieces.xlsx`,
whose pieces are listed, with review notes, in [pieces.md](pieces.md). How the work is to be done,
in stages, is in [roadmap.md](roadmap.md).

Every rule below is marked by where it stands:

- **Stated**: the designer has said so.
- **Proposed**: a suggestion, to fill a gap; not decided until the designer agrees.
- **Q*n***: an open question, listed in full in [§9](#9-open-questions).

## 1. Overview

**Stated.** One player creates a game, choosing (or leaving to chance) the board and how the armies
face each other. Each player, on joining, spends a budget of points on an army of their own: which
pieces, which of their restrictions to buy off, where their palace and throne stand, and where
every piece starts. Play then follows the classic rules, as far as they apply (**Q1**), with points
won in play to be spent on upgrades and promotions.

## 2. Creating a game

**Stated.** The creator *may* set each of these; whatever they leave unset is chosen at random.

| Setting | Values | Notes |
|---|---|---|
| Board size | width × length | Need not be square; must divide into four equal **kingdoms**, one per quarter. |
| Team game | yes / no | No: everyone for themselves. |
| Allies' kingdoms | same side / opposite corners | Same side: top against bottom, or left against right. Opposite corners: the classic pinwheel. |
| Rivers | north–south, east–west, both, neither | Drawn on the board where present. |
| Army points | a number | What each player has to spend on their army. |

- **Stated.** More kingdoms, or kingdoms of unequal size, may come later; not now.
- **Proposed.** "Four equal quarters" means both sides of the board are even. A smallest board
  (8 × 8?) and a largest (**Q2**) keep both the draft and the page workable.
- **Proposed.** A river runs along a midline, between two cells, so no cell is on the river; the
  north–south river separates the north kingdoms from the south ones. A river matters only to pieces
  whose rules name it (the Xiangqi Elephant and Soldier, so far). What "crossing the river" means
  for an army in a corner, which has a river on two sides when both are present, is **Q3**.
- Random values need ranges: **Q2**.

## 3. Joining: building an army

**Stated.** Each player, on joining, sees the board and its rivers, and then:

1. **Drafts pieces** from the [catalogue](pieces.md) (and perhaps more pieces later), each at its
   **Piece Value**.
2. **Buys upgrades** at draft time: paying a piece's removal cost lifts a restriction on it
   (movement restrictions and area restrictions are priced apart).
3. **Places a palace**: a 3 × 3 square entirely within their kingdom.
4. **Places a throne** on any cell of the palace; it need not be the centre.
5. **Places every drafted piece** on any cell of their kingdom.

- **Stated.** An infantry piece's **forward** is always toward the enemy. With allies in opposite
  corners, forward follows the classic pinwheel (rules.md §3) or its nearest equivalent.
- **Stated.** An infantry piece that does not start on its army's first or second row loses every
  power tied to starting there: a Western Pawn on row 3 may not step two squares on its first move,
  nor be taken *en passant*… (**Q4**: does it lose the power for good, or only until it is back on
  row 1–2?)
- Whether an army must have a leader, and how many, is **Q5**. Whether the same piece may be
  drafted more than once, and whether points not spent are kept, is **Q6**. Whether players draft
  and place in secret, all at once, or in turn and in view of each other, is **Q7**.
- **Proposed.** Forward on a rectangular board: in a same-side game, straight toward the far side;
  in a corner game, as in the classic game, the direction that leads out of the kingdom toward the
  next army round the board (**Q8** if the designer means otherwise).

## 4. Pieces

The catalogue is in [pieces.md](pieces.md). Each piece has a **class** (Leader, Infantry,
Cavalry / Leaper, Heavy / Slider, Specialty / Artillery), a move, a capture (most capture as they
move), and perhaps:

- a **movement restriction**, part of how the piece itself moves (a Horse's leg may be blocked),
  which a price lifts;
- an **area restriction**, on where it may go (only its palace, one colour of square, its own side
  of the river), which another price lifts;
- **special powers** (castling, the flying-general capture…);
- a **promotion**: what it may become, and when.

**Proposed.** The classic pieces (Raja, Elephant, Horse, Boat, Pawn) keep their own kinds for the
classic game. Whether they also join the grand catalogue is **Q9**; the grand Horse, Elephants and
Pawns move differently from the classic ones of the same names, so the program keeps their keys
apart whatever the answer.

## 5. Palaces and thrones

- **Stated.** Each army has one palace (3 × 3, inside its kingdom) holding its throne.
- **Stated.** A piece with the power "can move diagonally along marked palace lines" may move from
  any cell of any palace directly *toward* that palace's throne, even in a direction it could not
  otherwise take. The page marks these lines.
- **Q10.** What "directly toward the throne" means for a cell that is not on a line with it (a
  corner, when the throne is at the middle of an edge, is a knight's move away); and whether such a
  piece may also move *away* from the throne along the same line, as in Janggi.
- **Q11.** "Confined to the 3 × 3 palace": to its own army's palace, or to whichever palace it is
  in? The Xiangqi General's rule (§6.3) suggests the second.

## 6. Play

### 6.1 The classic rules

**Stated.** Play follows the classic rules and those still being worked on. Which of them carry
over, and how, is **Q1**; the largest parts of it are:

- **Dice.** In the classic game a die names the piece that may move. With an army of drafted pieces
  it cannot do that as it is. Do dice stay, mapped some other way (by class?), or does the grand
  game play without dice?
- **Throne control** (rules.md §6) was written for one Raja per army. With several leaders, which
  of them commands an army whose throne it holds?
- **Promotion and ransom** (rules.md §8) against the sheet's own promotions (§6.2 below).
- **How the game ends** (rules.md §10), and how it is won: by points, by the last team standing?

### 6.2 Points: scoring and spending

- **Stated.** Points won in play (by capture, and the like) may be spent on:
  - **upgrades** for an upgradable piece, lifting its restrictions;
  - **promotions** for a promotable piece, at the cost the new piece would have had in the draft.
- **Stated.** Either only while that piece is in a palace whose throne holds one of its player's
  own leaders; the palace need not be the player's own.
- **Stated.** A player whose own leaders sit on all four thrones at once wins a bonus of **60**
  points.
- **Q12.** What a capture scores: the piece's Piece Value (a leader's then being 60 or more)?
- **Q13.** Does spending reduce the score that decides the game, or is there a purse apart from the
  score?
- **Q14.** Does a promotion still need the sheet's condition (a Shogi piece in the far three ranks,
  a Western Pawn on the last rank) and a payment besides, or does the payment replace the
  condition? Does the classic "far edge, into any captured piece" rule still hold?
- **Q15.** Is the four-throne bonus won once, or again every time the state is entered afresh (as
  the classic empire bonus is)?

### 6.3 Leaders' powers

- **Stated — Xiangqi General, flying-general capture.** The General may capture any leader by the
  flying-general capture. Once he is outside every palace his confinement is suspended; it returns
  as soon as he enters any palace. Only a visit to a palace whose throne holds another of his
  player's leaders lets that player buy off the confinement; he cannot buy it off by himself.
  - **Q16.** When may the capture be made: along an open orthogonal line of any length (as in
    Xiangqi), or diagonally too? Does it use the General's move for the turn?
- **Stated — Janggi General, palace face-off.** The General may call a palace face-off against any
  enemy leader, even when it is not his turn: each leader is captured by the other's side at once,
  and both players score the capture.
  - **Q17.** When may it be called: when the two leaders face each other on an open line, or when
    both stand in palaces, or…? How long does the chance last, if it is not his turn — until the
    next move?
- **Stated — Western King, castling.** The King may castle with "the Rook": nothing between them,
  the two in one orthogonal line, and not castling across check. The King steps two squares toward
  the Rook, and the Rook goes to the King's far side. Castling is allowed even after either piece
  has moved, or castled before.
  - **Q18.** Which pieces count as "the Rook": only the Western Rook, or any orthogonal slider?
  - **Q19.** There is no check in the classic game (a Raja is simply captured). Does "not across
    check" mean the King may not start on, pass over, or land on a cell an enemy piece could move
    to?

## 7. Upgrades and restrictions

- **Stated.** A restriction may be lifted at draft time, at its listed cost, and later in play,
  under §6.2.
- Some restrictions in the sheet are not rules but geometry: a piece that moves only diagonally
  never leaves its colour, whatever is paid. See [pieces.md](pieces.md#review-notes), **Q20**.
- **Q21.** What a piece becomes once its movement restriction is lifted (a Horse whose leg may no
  longer be blocked is a Knight; a Shogi Pawn no longer blocked straight ahead moves… how?).

## 8. The page

- **Stated.** Every kind of piece looks different from every other, even if only slightly.
- **Stated.** A player may click any piece, their own or anyone's, to see what it is and what it can
  do.
- **Proposed.** The reminder is a card built from the catalogue: name, class, how it moves and
  captures, restrictions still on it (and bought off), powers, what it may promote to, and its
  value; with the cells it could reach marked on the board, as the page does now for the player's
  own pieces.
- **Proposed.** Each class gets its own outline, and each kind its own mark inside it, so that a
  piece's class can be told at a glance and its kind on a closer look. The two piece sets
  (traditional, chess) carry over.

## 9. Open questions

1. **Q1** Which classic rules carry over, and how: dice, throne control with several leaders,
   promotion and ransom, the end of the game and how it is won (§6.1).
2. **Q2** Smallest and largest board; ranges for each random setting (board, army points).
3. **Q3** What crossing the river means for an army in a corner, with one river or two.
4. **Q4** Does an infantry piece that started on row 3 or beyond lose its start-row powers for good?
5. **Q5** Must an army have a leader? How many may it have?
6. **Q6** May a piece be drafted more than once? Are points not spent in the draft kept?
7. **Q7** Drafting and placing: in secret and all at once, or in turn and in view?
8. **Q8** Forward, on a rectangular board, in each arrangement (§3).
9. **Q9** Do the classic pieces join the grand catalogue?
10. **Q10** Palace lines: "toward the throne" from a cell not in line with it; moving away along
    the line.
11. **Q11** Confinement: to the piece's own palace, or to whichever it is in?
12. **Q12** What a capture scores.
13. **Q13** Is the score also the purse?
14. **Q14** Promotion: condition and payment, or payment alone; the classic far-edge rule.
15. **Q15** The four-throne bonus: once, or every time?
16. **Q16** The flying-general capture: which lines; does it take the move?
17. **Q17** The palace face-off: when, and for how long, may it be called?
18. **Q18** Castling: which pieces count as the Rook?
19. **Q19** Castling: what "across check" means without check.
20. **Q20** Restrictions that geometry already imposes (§7).
21. **Q21** What lifting each movement restriction yields (§7).
22. **Q22** The catalogue's own questions, row by row: [pieces.md](pieces.md#review-notes).
