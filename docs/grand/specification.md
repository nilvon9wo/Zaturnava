# The grand game — Specification (v0.5 draft)

The [classic game](../rules.md) is one small part of a larger design, which has no name yet; this
document calls it **the grand game** until it does. The classic game stays available, unchanged,
as a variant of its own.

Sources: the designer's notes and answers of 2026-10-25, and the designer's spreadsheet of pieces,
listed in [pieces.md](pieces.md). How the work is to be done, in stages, is in
[roadmap.md](roadmap.md).

Every rule below is marked by where it stands:

- **Stated**: the designer has said so.
- **Proposed**: a suggestion, to fill a gap; not decided until the designer agrees.
- **Q*n***: an open question, listed in full in [§9](#9-open-questions).

## 1. Overview

**Stated.** One player creates a game, choosing (or leaving to chance) the board and how the armies
face each other. Each player, on joining, spends a budget of points on an army of their own: which
pieces, which of their restrictions to buy off, where their palace and throne stand, and where
every piece starts. Play then follows the classic rules, with dice naming a class of piece rather
than a piece, and with points won in play to be spent on upgrades and promotions.

## 2. Creating a game

**Stated.** The creator *may* set each of these; whatever they leave unset is chosen at random.

| Setting | Values | Notes |
|---|---|---|
| Board size | width × length | Need not be square. |
| Kingdom size | width × depth | Set apart from the board's size; one **kingdom** in each corner, as in the classic game (an 8 × 8 board with kingdoms of 4 × 2). |
| Team game | yes / no | No: everyone for themselves. |
| Allies' kingdoms | same side / opposite corners | Same side: neighbouring corners. Opposite corners: the classic pinwheel. |
| Rivers | north–south, east–west, both, neither | Drawn on the board where present. |
| Army points | a number | What each player has to spend on their army. |
| Drafting | in secret, all at once / in turn, in view | How players build and place their armies (§3). |

- **Stated.** For now, the four kingdoms are alike and each starts in a corner of the board. More
  kingdoms, kingdoms of unequal size, or kingdoms elsewhere may come later.
- **Stated.** With the kingdom's size set apart from the board's, the setup can keep every
  army a distance from the enemy, and from any promotion zone.
- **Proposed.** Kingdoms may not overlap or touch, so that there is open ground between them. A
  smallest and a largest board and kingdom (**Q1**) keep both the draft and the page workable.
- **Q24.** Which way a kingdom of width × depth lies in each corner: as in the classic pinwheel
  (each kingdom turned a quarter from the last, so that each army's back rank lies along a
  different edge), or with every kingdom's back rank along the top or bottom edge?
- **Proposed.** A river runs along a midline, between two cells, so no cell is on the river; the
  north–south river separates the north kingdoms from the south ones. A river matters only to pieces
  whose rules name it (the Xiangqi Elephant and Soldier, so far). What "crossing the river" means
  for an army in a corner, which has a river on two sides when both are present, is **Q2**.
- Random values need ranges: **Q1**.

## 3. Joining: building an army

**Stated.** Each player, on joining, sees the board and its rivers, and then:

1. **Drafts pieces** from the [catalogue](pieces.md) (and perhaps more pieces later), each at its
   **Piece Value**.
2. **Buys upgrades** at draft time: paying a piece's lift cost removes a restriction on it
   (movement restrictions and area restrictions are priced apart).
3. **Places a palace**: a 3 × 3 square within their kingdom (or, where it does not fit, as **Q23**
   settles).
4. **Places a throne** on any cell of the palace; it need not be the centre.
5. **Places every drafted piece** on any cell of their kingdom.

- **Stated.** A player may draft as many leaders as they can afford. Leaders are dear (60 points
  and more), and some pieces promote into further leaders.
- **Stated.** Points not spent on the army are kept: the player starts the game with them as their
  score.
- **Stated.** Whether players draft and place in secret and all at once, or in turn and in view of
  each other, is a setting of the game (§2).
- **Stated.** An infantry piece's **forward** is always toward the enemy. With allies in opposite
  corners, forward follows the classic pinwheel (rules.md §3) or its nearest equivalent.
- **Stated.** An infantry piece that does not start on its army's first or second row loses every
  power tied to starting there: a Western Pawn on row 3 may not step two squares on its first move,
  nor be taken *en passant*… (**Q3**: does it lose the power for good, or only until it is back on
  row 1–2?)
- **Q4.** May an army have no leader at all? May the same piece be drafted more than once?
- **Q5.** May a player buy a second palace, on a board large enough, and at what price? The
  designer's view so far: more than one throne at the start may confuse.
- **Proposed.** Forward on a rectangular board: in a same-side game, straight toward the far side;
  in a corner game, as in the classic game, the direction that leads out of the kingdom toward the
  next army round the board (**Q6** if the designer means otherwise).

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

**Stated.** A restriction that the piece's own move imposes (a piece that moves only diagonally
never leaves its colour) is listed, but cannot be bought off.

**Proposed.** The classic pieces (Raja, Elephant, Horse, Boat, Pawn) keep their own kinds for the
classic game. Whether they also join the grand catalogue is **Q7**; the grand Horse, Elephants and
Pawns move differently from the classic ones of the same names, so the program keeps their keys
apart whatever the answer.

## 5. Palaces and thrones

- **Stated.** Each army has one palace (3 × 3, inside its kingdom) holding its throne.
- **Q23.** A kingdom less than 3 deep (the classic 4 × 2) cannot hold a palace. The designer's
  options, perhaps a setting of the game:
  - **(A)** the palace shrinks to fit;
  - **(B)** the palace may stand partly off the board, adding those cells to the board;
  - **(C)** the palace may stand partly outside the kingdom, on the open board, if it lies along
    the board's edge and most of it is inside (with a 4 × 2 kingdom: 6 cells in, 3 out).
- **Proposed** for the first version: kingdoms are at least 3 × 3, so that a palace always fits;
  then (C). Of the three, (C) alone keeps the palace whole (palace lines and confinement are
  written for 3 × 3) and the board rectangular (rivers and drawing assume it); (B) changes the
  board's shape, and (A) the palace's.
- **Stated.** A piece with the power "can move diagonally along marked palace lines" may move from
  any cell of any palace directly *toward* that palace's throne, even in a direction it could not
  otherwise take. The page marks these lines.
- **Q8.** What "directly toward the throne" means for a cell that is not on a line with it (a
  corner, when the throne is at the middle of an edge, is a knight's move away); and whether such a
  piece may also move *away* from the throne along the same line, as in Janggi.
- **Q9.** "Confined to the 3 × 3 palace": to its own army's palace, or to whichever palace it is
  in? The Xiangqi General's rule (§6.4) suggests the second.

## 6. Play

### 6.1 The classic rules

**Stated.** Play follows the classic rules ([rules.md](../rules.md)), and those still being worked
on, with what follows in place of the Raja-and-dice parts.

- **Stated — dice.** A die names a **class** of piece (Leader, Cavalry, Heavy, Infantry,
  Artillery), not a kind: the player may move any piece of that class.
  - **Stated.** On the default two six-sided dice, as the classic faces map: 1 Artillery,
    2 Cavalry, 3–4 Heavy, 5–6 Infantry or Leader (the player's choice).
- **Stated — throne control.** What the classic rules say of a Raja holds of every leader. A
  player whose leaders sit on several thrones commands (or is made to share the turn of) each of
  those armies, as in rules.md §6.
- **Stated — a leaderless army.** A player with no leader left, whose ally has one, is commanded by
  that ally, as in rules.md §6.1.
- **Stated — promotion at the far edge.** Any Infantry piece that reaches the far edge may be
  promoted into any captured piece, as a classic Pawn is (rules.md §8).
- **Stated — the end.** The game ends as the classic game does (rules.md §10). Points decide it,
  unless the end itself does: a team with no opposition left wins.

### 6.2 Points: scoring and spending

- **Stated.** Capturing a piece scores its value as listed in the catalogue: upgrades bought for
  it add nothing. A promoted piece scores as what it was promoted into, not what it was.
- **Stated.** Points may be spent, in play, on:
  - **upgrades** for an upgradable piece, lifting its restrictions;
  - **promotions** for a promotable piece, at the full value of the piece it becomes (nothing off
    for what it was).
- **Stated.** Either only while that piece is in a palace whose throne holds one of its player's
  own leaders; the palace need not be the player's own.
- **Stated.** Spending lowers the player's score: if the score decides the game, what is spent is
  lost to it.
- **Stated.** A player whose own leaders sit on all four thrones at once wins a bonus of **60**
  points. **Q12**: once, or again every time the state is entered afresh (as the classic empire
  bonus is)?
- **Stated — a piece's own promotion.** A piece that meets its own promotion condition (a Shogi
  piece in the far three ranks, a Makruk Pawn on the sixth rank, a Western Pawn on the last rank)
  is promoted free.
- **Stated.** Every promotion is optional, and may be put off for as long as its condition holds:
  a player who expects the piece to be captured at once need not hand its captor the dearer score.
- **Stated.** An Infantry piece with a promotion of its own (a Western Pawn) chooses, at the far
  edge, between that promotion and becoming any captured piece.

### 6.3 Other classic scores

The classic bonuses (entering an enemy throne, taking a leader on its own throne, the empire) were
reckoned against pieces worth 1 to 5. **Stated.** Whether they stay as they are, beside captures
worth up to 75, or scale, is for playtesting to show; so is the leader bonus of 60, which is meant
to make players guard their leaders even when they are not using them to take thrones.

### 6.4 Leaders' powers

- **Stated — Xiangqi General, flying-general capture.** The General may capture any leader by the
  flying-general capture. Once he is outside every palace his confinement is suspended; it returns
  as soon as he enters any palace. Only a visit to a palace whose throne holds another of his
  player's leaders lets that player buy off the confinement; he cannot buy it off by himself.
  - **Q15.** When may the capture be made: along an open orthogonal line of any length (as in
    Xiangqi), or diagonally too? Which die allows it, as a Leader's move?
- **Stated — Janggi General, palace face-off.** The General may call a palace face-off against any
  enemy leader, even when it is not his turn: each leader is captured by the other's side at once,
  and both players score the capture.
  - **Q16.** When may it be called: when the two leaders face each other on an open line, or when
    both stand in palaces, or…? How long does the chance last, if it is not his turn — until the
    next move?
- **Stated — Western King, castling.** The King may castle with "the Rook": nothing between them,
  the two in one orthogonal line, and not castling across check. The King steps two squares toward
  the Rook, and the Rook goes to the King's far side. Castling is allowed even after either piece
  has moved, or castled before.
  - **Q17.** Which pieces count as "the Rook": only the Western Rook, or any orthogonal slider?
  - **Q18.** There is no check in the classic game (a Raja is simply captured). Does "not across
    check" mean the King may not start on, pass over, or land on a cell an enemy piece could move
    to? And castling moves two classes at once: which die allows it?

## 7. Upgrades and restrictions

- **Stated.** A restriction may be lifted at draft time, at its listed cost, and later in play,
  under §6.2; one that the piece's own move imposes, never.
- **Stated.** A piece bound to one colour (or a smaller share of the board) by its own move can
  never reach the rest, however it is moved: a leader that keeps to the other colour is safe from
  it. Its value shows that, by the share of the board it can reach ([pieces.md](pieces.md#binding)).
- **Q19.** What a piece becomes once its movement restriction is lifted (a Horse whose leg may no
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
  piece's class can be told at a glance (and with it, which die moves it) and its kind on a closer
  look. The two piece sets (traditional, chess) carry over.

## 9. Open questions

Answered on 2026-10-25, and folded in above: dice, leaders and throne control, scoring and
spending, unspent points, promotion at the far edge and its price, drafting in secret or in view.
The numbers stay as they are; answered questions are marked so.

1. **Q1** Smallest and largest board; ranges for each random setting (board, army points).
2. **Q2** What crossing the river means for an army in a corner, with one river or two.
3. **Q3** Does an infantry piece that started on row 3 or beyond lose its start-row powers for good?
4. **Q4** May an army have no leader at all? May a piece be drafted more than once?
5. **Q5** A second palace: may it be bought, and at what price?
6. **Q6** Forward, on a rectangular board, in each arrangement (§3).
7. **Q7** Do the classic pieces join the grand catalogue?
8. **Q8** Palace lines: "toward the throne" from a cell not in line with it; moving away along
   the line.
9. **Q9** Confinement: to the piece's own palace, or to whichever it is in?
10. **Q10** *Answered:* which die faces name which class (§6.1).
11. **Q11** *Answered:* how the game ends, and how it is won (§6.1).
12. **Q12** The four-throne bonus: once, or every time?
13. **Q13** *Answered:* promotion by a piece's own condition is free, optional and may wait (§6.2).
14. **Q14** *For playtesting:* the classic bonuses, and the leader bonus of 60 (§6.3).
15. **Q15** The flying-general capture: which lines; which die.
16. **Q16** The palace face-off: when, and for how long, may it be called?
17. **Q17** Castling: which pieces count as the Rook?
18. **Q18** Castling: what "across check" means without check; which die.
19. **Q19** What lifting each movement restriction yields (§7).
20. **Q20** The catalogue's own questions: [pieces.md](pieces.md#still-open).
21. **Q21** *Answered:* a bound piece's value, by the share of the board it can reach (§7).
22. **Q22** *Answered:* the Makruk Pawn promotes to the Courtier; the Private is gone.
23. **Q23** A palace that does not fit in its kingdom: shrink it, let it off the board, or let it
    out of the kingdom (§5).
24. **Q24** Which way a kingdom lies in each corner (§2).
