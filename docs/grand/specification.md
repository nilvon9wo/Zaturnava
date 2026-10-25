# The grand game — Specification (v0.7 draft)

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
| Kingdoms' facing | pinwheel / top and bottom | Pinwheel (the default): each kingdom turned a quarter from the last, so that each army's back rank lies along a different edge. Top and bottom: every back rank along the top or bottom edge. |
| Rivers | north–south, east–west, both, neither | Drawn on the board where present. |
| Army points | a number | What each player has to spend on their army. |
| Drafting | in secret, all at once / in turn, in view | How players build and place their armies (§3). |

- **Stated.** For now, the four kingdoms are alike and each starts in a corner of the board. More
  kingdoms, kingdoms of unequal size, or kingdoms elsewhere may come later.
- **Stated.** With the kingdom's size set apart from the board's, the setup can keep every
  army a distance from the enemy, and from any promotion zone.
- **Stated.** The smallest game is a 6 × 6 board with four 3 × 3 kingdoms, which touch. Kingdoms
  may not overlap.
- **Stated.** There need be no largest board in the rules: some players may want 100 × 100 with
  12 × 12 kingdoms. **Proposed:** the first version allows up to 26 × 26 (cells named a to z, and
  the whole board on one screen); larger boards wait for a page that zooms and scrolls, and for
  playtesting to show what bots and players can manage (**Q1**).
- **Stated.** Which way the kingdoms lie in their corners is a setting: the classic pinwheel by
  default, or every back rank along the top or bottom edge.
- **Stated.** A river halves the board, or two rivers quarter it, as the game is set, and the page
  shows them. A river matters only to the pieces whose rules name it: those that may not cross it
  (the Xiangqi Elephant), or that change when they have crossed it (the Xiangqi Soldier). Later, a
  river might wind.
- **Proposed.** A river runs along a midline, between two rows of cells, so no cell is on it.
- **Proposed** (**Q2**). With two rivers, a kingdom in a corner has a river on two sides. A piece
  that may not cross *the* river may cross *neither*: the Xiangqi Elephant keeps to its own quarter.
  A piece changed by crossing it is changed by crossing *either*: the Xiangqi Soldier steps sideways
  once it is across a river from its kingdom. With no river at all, neither rule has anything to
  apply to: the Elephant goes anywhere, and the Soldier never steps sideways.
- Random values need ranges: **Q1**. **Proposed:** board 8 × 8 to 16 × 16; kingdom 3 × 3 to half
  the board; army points 120 to 240, so that a leader is always within reach, and two sometimes.

## 3. Joining: building an army

**Stated.** Each player, on joining, sees the board and its rivers, and then:

1. **Drafts pieces** from the [catalogue](pieces.md) (and perhaps more pieces later), each at its
   **Piece Value**.
2. **Buys upgrades** at draft time: paying a piece's lift cost removes a restriction on it
   (movement restrictions and area restrictions are priced apart).
3. **Places a palace**: a 3 × 3 square within their kingdom.
4. **Places a throne** on any cell of the palace; it need not be the centre.
5. **Places every drafted piece** on any cell of their kingdom.

- **Stated.** A player may draft as many leaders as they can afford. Leaders are dear (60 points
  and more), and some pieces promote into further leaders.
- **Stated.** Points not spent on the army are kept: the player starts the game with them as their
  score.
- **Stated.** Whether players draft and place in secret and all at once, or in turn and in view of
  each other, is a setting of the game (§2).
- **Stated.** An infantry piece's **forward** is toward the enemy; with the kingdoms in a pinwheel,
  its place on the pinwheel sets it, as in the classic game (rules.md §3). That is, forward is away
  from its kingdom's back rank.
- **Stated.** Start-row powers make up for the distance to cross, and speed the game. An infantry
  piece placed beyond its army's first two rows has been brought forward already, and has none of
  them: a Western Pawn on row 3 may not step two squares on its first move, nor take *en passant*.
  **Stated:** how many rows count as start rows (2) may be a setting of the game.
  **Proposed** (**Q3**): a piece that starts without them never gains them.
- **Stated.** An army may have no leader, as an army in the classic game may lose its Raja and play
  on. A player who starts without one plays as one whose leader has been captured, except that
  there is no leader to ransom, to escape, or to promote into.
- **Stated.** A player may draft any pieces the budget allows: several leaders, and several of one
  kind, leaders included.
- **Q5.** May a player buy a second palace, on a board large enough, and at what price? Left open.
  A palace without a throne gives little (a few more moves for few pieces, and a cage for others);
  a palace is worth its place as the seat of a leader, which its throne gives pieces a reason to
  guard. A second throne would raise who rules when two thrones of one army are held by different
  players.

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

**Stated.** The classic pieces do not join the grand catalogue, which already has their moves: the
Raja moves as the King (without castling), the classic Elephant as the Rook, the classic Horse as
the Knight, the Boat as the Alfil, the classic Pawn as the Makruk Pawn (without its own promotion).
They may be added later. The program keeps their keys apart from the grand pieces' all the same.

## 5. Palaces and thrones

- **Stated.** Each army has one palace (3 × 3, inside its kingdom) holding its throne.
- **Stated.** In the first version a kingdom is at least 3 × 3, so that a palace always fits.
- **Stated.** Later, a kingdom too small for a palace (the classic 4 × 2) lets the palace stand
  partly outside it, on the open board, if the palace lies along the board's edge and most of it
  is inside (with a 4 × 2 kingdom: 6 cells in, 3 out). Shrinking the palace, or letting it stand off
  the board, were the other choices: the first would break palace lines and confinement, written
  for 3 × 3, and the second the board's rectangle, which rivers and drawing assume.
- **Stated.** Later still, palaces may be turned off, leaving bare thrones. The pieces bound to a
  palace (confined to it, or with powers in it) are then left out of the draft.
- **Stated.** Every palace has a line from each of its cells to its throne, and the page draws
  them. A piece with the power "can move diagonally along marked palace lines" may follow them,
  even in a direction it could not otherwise take.
- **Proposed** (**Q8**). A step along a line goes to the next cell whose centre the line passes
  through, toward the throne or away from it. Most lines are straight or diagonal, and the step is
  one cell. A line from a cell a knight's move from the throne (a corner, when the throne is in the
  middle of an edge) passes through no other cell's centre, so the step goes straight to the throne.
- **Stated.** A piece confined to a palace must start in one: since a player places their army in
  their own kingdom, at most nine such pieces fit, in their own palace.
- **Proposed** (**Q9**). A confined piece is confined to the palace it is in. Only one that has
  left its palace (the Xiangqi General, by its flying-general capture) or bought off its confinement
  can ever be in another.

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
  points, once for each set of leaders that does it: not again for walking the same leaders off
  and back on.
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
  - **Proposed** (**Q15**). As in Xiangqi: along an open orthogonal line, of any length, with
    nothing between the two; the General moves onto the captured leader's cell. It is the General's
    move, so a Leader die allows it, or a move made without rolling.
- **Stated — Janggi General, palace face-off.** The General may call a palace face-off against any
  enemy leader, even when it is not his turn: each leader is captured by the other's side at once,
  and both players score the capture.
  - **Proposed** (**Q16**). As Janggi's *bikjang*, which this is: when the General and an enemy
    leader face each other along an open orthogonal line, with nothing between. The chance comes
    whenever a move, anyone's, makes it so, and lasts until the next move is made; the page offers
    it to the General's player, as it offers a ransom, and a bot answers at once. Does either
    leader have to be in a palace, as "palace face-off" suggests?
- **Stated — Western King, castling.** The King may castle with "the Rook": nothing between them,
  the two in one orthogonal line, and not castling across check. The King steps two squares toward
  the Rook, and the Rook goes to the King's far side. Castling is allowed even after either piece
  has moved, or castled before.
  - **Proposed** (**Q17**). Only the Western Rook: with the Chancellor or the Queen, castling would
    move a far stronger piece for free.
  - **Proposed** (**Q18**). There is no check in this game (a leader is simply captured), so "not
    across check" means that the King may not start on, pass over, or land on a cell an enemy piece
    could capture on. Castling is the King's move, so a Leader die allows it, or a move made without
    rolling.

## 7. Upgrades and restrictions

- **Stated.** A restriction may be lifted at draft time, at its listed cost, and later in play,
  under §6.2; one that the piece's own move imposes, never.
- **Stated.** A piece bound to one colour (or a smaller share of the board) by its own move can
  never reach the rest, however it is moved: a leader that keeps to the other colour is safe from
  it. Its value shows that, by the share of the board it can reach ([pieces.md](pieces.md#binding)).
- **Stated.** A piece whose restriction is lifted stays the piece it was: it keeps its kind, and
  only has more legal moves, the restriction no longer applying to it.

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

1. **Q1** *Partly answered:* the smallest game (§2). The first version's largest, and the random
   ranges, are proposed.
2. **Q2** Rivers and a corner kingdom; no river at all (§2, proposed).
3. **Q3** *Partly answered:* a piece that starts beyond the start rows has no start-row powers;
   that it never gains them is proposed (§3).
4. **Q4** *Answered:* an army may have no leader, and pieces may be drafted more than once (§3).
5. **Q5** A second palace: may it be bought, and at what price? Left open (§3).
6. **Q6** *Answered:* forward is away from the kingdom's back rank (§3).
7. **Q7** *Answered:* the classic pieces do not join the catalogue, for now (§4).
8. **Q8** Palace lines: a step along one (§5, proposed).
9. **Q9** Confinement: to the palace the piece is in (§5, proposed).
10. **Q10** *Answered:* which die faces name which class (§6.1).
11. **Q11** *Answered:* how the game ends, and how it is won (§6.1).
12. **Q12** *Answered:* the four-throne bonus, once for each set of leaders (§6.2).
13. **Q13** *Answered:* promotion by a piece's own condition is free, optional and may wait (§6.2).
14. **Q14** *For playtesting:* the classic bonuses, and the leader bonus of 60 (§6.3).
15. **Q15** The flying-general capture (§6.4, proposed).
16. **Q16** The palace face-off (§6.4, proposed); must a leader be in a palace?
17. **Q17** Castling: only with the Western Rook (§6.4, proposed).
18. **Q18** Castling: "across check" without check (§6.4, proposed).
19. **Q19** *Answered:* a piece with a restriction lifted stays itself, with more moves (§7).
20. **Q20** The catalogue's own questions: [pieces.md](pieces.md#still-open).
21. **Q21** *Answered:* a bound piece's value, by the share of the board it can reach (§7).
22. **Q22** *Answered:* the Makruk Pawn promotes to the Courtier; the Private is gone.
23. **Q23** *Answered:* a palace that does not fit its kingdom (§5).
24. **Q24** *Answered:* which way a kingdom lies in its corner is a setting (§2).
