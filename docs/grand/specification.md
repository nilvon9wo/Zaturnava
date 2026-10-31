# The grand game — Specification (v0.8 draft)

[Chaturaji 2027](../rules.md) is one small part of a larger design, **the grand game**. Its proposed
name is **Grand Legions** (the designer, 2026-10-31, in place of "The Grand Draft", which put the
drafting before the play); the program calls it so already (`Zaturnava.Games.GrandLegions`). Chaturaji
2027 stays available, unchanged, as a variant of its own.

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
every piece starts. Play then follows the Chaturaji 2027 rules, with dice naming a class of piece rather
than a piece, and with points won in play to be spent on upgrades and promotions.

## 2. Creating a game

**Stated.** The creator *may* set each of these; whatever they leave unset is chosen at random.

| Setting | Values | Notes |
|---|---|---|
| Board size | width × length | Need not be square. |
| Kingdom size | width × depth | Set apart from the board's size; one **kingdom** in each corner, as in Chaturaji 2027 (an 8 × 8 board with kingdoms of 4 × 2). |
| Team game | yes / no | No: everyone for themselves. |
| Allies | any two of the four seats | In a team game, the creator picks two seats as allies; the other two are allies by implication. |
| Kingdoms' facing | pinwheel / top and bottom | Pinwheel (the default): each kingdom turned a quarter from the last, so that each army's back rank lies along a different edge. Top and bottom: every back rank along the top or bottom edge. |
| Rivers | none, halving the board vertically or horizontally, or both | Drawn on the board where present. Later, perhaps: a path of the creator's own, or a random one. |
| River's course | through kingdoms / around kingdoms | Only for a kingdom reaching past the middle of the board (§2). |
| Army points | a number | What each player has to spend on their army. |
| Drafting | in secret, all at once / in turn, in view | How players build and place their armies (§3). |

- **Stated.** For now, the four kingdoms are alike and each starts in a corner of the board. More
  kingdoms, kingdoms of unequal size, or kingdoms elsewhere may come later.
- **Stated.** With the kingdom's size set apart from the board's, the setup can keep every
  army a distance from the enemy, and from any promotion zone.
- **Stated.** The smallest game is a 6 × 6 board with four 3 × 3 kingdoms, which touch. Kingdoms
  may not overlap.
- **Stated.** There need be no largest board in the rules: some players may want 100 × 100 with
  12 × 12 kingdoms. **Stated:** the first version allows up to 26 × 26 (cells named a to z, and
  the whole board on one screen); larger boards wait for a page that zooms and scrolls, and for
  playtesting to show what bots and players can manage.
- **Stated.** Which way the kingdoms lie in their corners is a setting: Chaturaji 2027's pinwheel by
  default, or every back rank along the top or bottom edge.
- **Stated.** A river halves the board, or two rivers quarter it, as the game is set, and the page
  shows them with blue lines, wherever they run. A river matters only to the pieces whose rules name
  it: those that may not cross it (the Xiangqi Elephant), or that change when they have crossed it
  (the Xiangqi Soldier). Later, a river might wind, along a path the creator draws or one drawn at
  random.
- **Stated (2026-10-31).** The blue lines come with the rivers, from the first version that has
  rivers: players should never have to remember, or guess, where a river is.
- **Stated (2026-10-31).** On a side with an even number of cells, a river runs between the two
  middle rows of cells, so no cell is on it. On an odd side, it runs through the middle row, cutting
  its cells in half: a piece that may not cross the river may not enter those cells.
- **Stated (2026-10-31).** Where a kingdom reaches past the middle of the board, the creator chooses
  whether the river runs through the kingdom, or follows its outline, so that no move within the
  kingdom crosses it.
  - **Stated (2026-10-31).** A cell is across a river from a kingdom when the river lies between the
    cell and the kingdom's corner. Across means fully clear of the river: a cell the river cuts in
    half is not yet across.
- **Stated (2026-10-31).** Later, perhaps: bridges, and pieces that build them; wide rivers, which
  some pieces cannot cross in a single move.
- **Stated (2026-10-31).** The creator picks the allies on a picture: four coloured squares arranged
  as the seats are, of which they choose two; the other two are allied by implication. Naming them
  "same side" or "top and bottom" would confuse, since the board effectively spins.
  - **Stated (2026-10-31).** Later, perhaps: one player with an ally while the others have none;
    alliances made and broken during play; and games with other numbers of players, two the easiest.
- **Stated.** With two rivers, a kingdom in a corner has a river on two sides. A piece
  that may not cross *the* river may cross *neither*: the Xiangqi Elephant keeps to its own quarter.
  A piece changed by crossing it is changed by crossing *either*: the Xiangqi Soldier steps sideways
  once it is across a river from its kingdom. With no river at all, neither rule has anything to
  apply to: the Elephant goes anywhere, and the Soldier never steps sideways.
- **Stated.** Random values range over: board 8 × 8 to 16 × 16; kingdom 3 × 3 to half
  the board; army points 120 to 240, so that a leader is always within reach, and two sometimes.

### 2.1 Saved settings

- **Stated (2026-10-31).** With this many settings, setting them all every time is tedious. A player
  may name a set of settings, save it, pick it to start a game, and edit it before playing.
- **Stated (2026-10-31).** Other players may search and browse the saved settings. They may not
  edit someone else's set, but they may copy it, and edit the copy.
- **Q26** Do saved settings need versions, or a check for duplicates? Not important yet. The
  designer's thoughts (2026-10-31), for when it is:
  - A player who picks a set its owner has since edited may be frustrated to find it changed; but
    no one should have to remember version numbers, or dig through the details to see what changed.
    Perhaps a player may keep a set they played and liked in their own list, so it stays as they
    played it.
  - Duplicate sets, and sets never played, take up storage. Sets (or versions of them) not played
    for a while, a month say (a setting of the server), might be purged. Keeping them longer might
    one day be a premium offer, but for now nobody pays anybody for anything.

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
  its place on the pinwheel sets it, as in Chaturaji 2027 (rules.md §3). That is, forward is away
  from its kingdom's back rank.
- **Stated.** Start-row powers make up for the distance to cross, and speed the game. An infantry
  piece placed beyond its army's first two rows has been brought forward already, and has none of
  them: a Western Pawn on row 3 may not step two squares on its first move, nor take *en passant*.
  **Stated:** how many rows count as start rows (2) may be a setting of the game.
  **Stated:** a piece that starts without them never gains them.
- **Stated.** An army may have no leader, as an army in Chaturaji 2027 may lose its Raja and play
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

**Stated.** The Chaturaji 2027 pieces do not join the grand catalogue, which already has their moves: the
Raja moves as the King (without castling), its Elephant as the Rook, its Horse as
the Knight, the Boat as the Alfil, its Pawn as the Makruk Pawn (without its own promotion).
They may be added later. The program keeps their keys apart from the grand pieces' all the same.

## 5. Palaces and thrones

- **Stated.** Each army has one palace (3 × 3, inside its kingdom) holding its throne.
- **Stated.** In the first version a kingdom is at least 3 × 3, so that a palace always fits.
- **Stated.** Later, a kingdom too small for a palace (Chaturaji 2027's 4 × 2) lets the palace stand
  partly outside it, on the open board, if the palace lies along the board's edge and most of it
  is inside (with a 4 × 2 kingdom: 6 cells in, 3 out). Shrinking the palace, or letting it stand off
  the board, were the other choices: the first would break palace lines and confinement, written
  for 3 × 3, and the second the board's rectangle, which rivers and drawing assume.
- **Stated.** Later still, palaces may be turned off, leaving bare thrones. The pieces bound to a
  palace (confined to it, or with powers in it) are then left out of the draft.
- **Stated.** Every palace has a line from each of its cells to its throne, and the page draws
  them. A piece with the power "can move diagonally along marked palace lines" may follow them,
  even in a direction it could not otherwise take.
- **Stated.** A step along a line goes to the next cell the line passes through (touching a cell's
  corner is not passing through it), toward the throne or away from it. A line from a cell a
  knight's move from the throne (a corner, when the throne is in the middle of an edge) passes
  through two cells on the way, so it takes three steps. Counting only the cells whose centres the
  line passes through (so that such a line is one step, straight to the throne) may be a setting
  of the game.
- **Stated.** A piece confined to a palace must start in one: since a player places their army in
  their own kingdom, at most nine such pieces fit, in their own palace.
- **Stated.** A confined piece is confined to the palace it is in. Only one that has left its
  palace (the Xiangqi General, by its flying-general capture) or bought off its confinement can
  ever be in another, and one that bought it off is not confined in any palace.

## 6. Play

### 6.1 The Chaturaji 2027 rules

**Stated.** Play follows the Chaturaji 2027 rules ([rules.md](../rules.md)), and those still being worked
on, with what follows in place of the Raja-and-dice parts.

- **Stated (2026-10-28) — what carries over.** Everything Chaturaji 2027 has is in the grand game too:
  thrones, command, the empire, ransom, escape, and the rest. The grand game only adds to it. The one
  thing missing is the Boat, and with it the Boat triumph (rules.md §9). So a bot's sense of
  Chaturaji 2027 (what thrones, command, the empire and ransom are worth) serves both games, and the
  grand game's bots add their own for the catalogue's pieces and powers.
- **Stated (2026-10-29) — the back-to-back swap.** All three Pawns (Shogi, Western, Makruk) may swap
  places back to back, as two Chaturaji 2027 Pawns may (rules.md §3): they share its forward-only
  move.
  - **Stated (2026-10-29).** By the same reason, so may any other piece whose normal move is
    forward only, such as the Lance; no piece that may move sideways or back.
  - **Stated (2026-10-29).** Any two pieces that may swap back to back may swap with each other,
    whatever their kinds or classes: a Shogi Pawn with a Western Pawn, a Pawn with a Lance.

- **Stated — dice.** A die names a **class** of piece (Leader, Cavalry, Heavy, Infantry,
  Artillery), not a kind: the player may move any piece of that class.
  - **Stated.** On the default two six-sided dice, as Chaturaji 2027's faces map: 1 Artillery,
    2 Cavalry, 3–4 Heavy, 5–6 Infantry or Leader (the player's choice).
- **Stated — throne control.** What the Chaturaji 2027 rules say of a Raja holds of every leader. A
  player whose leaders sit on several thrones commands (or is made to share the turn of) each of
  those armies, as in rules.md §6.
- **Stated — a leaderless army.** A player with no leader left, whose ally has one, is commanded by
  that ally, as in rules.md §6.1.
- **Stated — promotion at the far edge.** Any Infantry piece that reaches the far edge may be
  promoted into any captured piece, as a Chaturaji 2027 Pawn is (rules.md §8).
- **Stated — the end.** The game ends as Chaturaji 2027 does (rules.md §10). Points decide it,
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

### 6.3 Other Chaturaji 2027 scores

The Chaturaji 2027 bonuses (entering an enemy throne, taking a leader on its own throne, the empire,
commanding every piece, a ransom's point for each prisoner, 2 for a Raja) were reckoned against pieces worth
1 to 5.

- **Stated (2026-10-29).** In the Grand game every one of them is **multiplied by 10**, so that they
  keep their meaning beside captures worth up to 75: entering an enemy throne scores 10, taking a
  leader on its own throne 20, the empire 10 (plus 20, or 40 on the throne, for its captor),
  commanding every piece 20, and each prisoner a ransom sends home 10, or 20 for any leader. Piece
  values stay as they are: dividing them by 10 instead would need fractions.
- **Stated.** Playtesting may still adjust any bonus on its own; so too the leader bonus of 60,
  which is meant to make players guard their leaders even when they are not using them to take
  thrones.

### 6.4 Leaders' powers

- **Stated — Xiangqi General, flying-general capture.** The General may capture any leader by the
  flying-general capture. Once he is outside every palace his confinement is suspended; it returns
  as soon as he enters any palace. Only a visit to a palace whose throne holds another of his
  player's leaders lets that player buy off the confinement; he cannot buy it off by himself.
  - **Stated.** As in Xiangqi: along an open orthogonal line, of any length, with
    nothing between the two; the General moves onto the captured leader's cell. It is the General's
    move, so a Leader die allows it, or a move made without rolling.
- **Stated — Janggi General, leader face-off.** The General may call a leader face-off against any
  enemy leader, even when it is not his turn: each leader is captured by the other's side at once,
  and both players score the capture. Neither need be in a palace: the General may have bought off
  his confinement, and the other leader may never have had one.
  - **Stated.** As Janggi's *bikjang*: when the General and an enemy leader face each other along
    an open orthogonal line, with nothing between. It is a choice, not automatic: the chance comes
    whenever a move, anyone's, makes it so, and lasts until the next move is made; the page offers
    it to the General's player, as it offers a ransom, and a bot answers at once.
- **Stated — Western King, castling.** The King may castle with "the Rook": nothing between them,
  the two in one orthogonal line, and not castling across check. The King steps two squares toward
  the Rook, and the Rook goes to the King's far side. Castling is allowed even after either piece
  has moved, or castled before.
  - **Stated.** Only with a Western Rook: a player who has none, never bought or since lost, cannot
    castle. A Rook gained by promotion (a Western Pawn's) counts as any other.
  - **Stated.** There is no check in this game (a leader is simply captured), so "not
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
- **Stated 2026-10-31, subject to playtesting.** The reminder is a card built from the catalogue: name, class, how it moves and
  captures, restrictions still on it (and bought off), powers, what it may promote to, and its
  value; with the cells it could reach marked on the board, as the page does now for the player's
  own pieces.
- **Stated 2026-10-31, subject to playtesting.** Each class gets its own outline, and each kind its own mark inside it, so that a
  piece's class can be told at a glance (and with it, which die moves it) and its kind on a closer
  look. The two piece sets (traditional, chess) carry over.

## 9. Open questions

Answered on 2026-10-25, and folded in above: dice, leaders and throne control, scoring and
spending, unspent points, promotion at the far edge and its price, drafting in secret or in view.
The numbers stay as they are; answered questions are marked so.

1. **Q1** *Answered:* the smallest and largest games, and the random ranges (§2).
2. **Q2** *Answered:* rivers and a corner kingdom; no river at all (§2).
3. **Q3** *Answered:* start rows, and a piece placed beyond them (§3).
4. **Q4** *Answered:* an army may have no leader, and pieces may be drafted more than once (§3).
5. **Q5** A second palace: may it be bought, and at what price? Left open (§3).
6. **Q6** *Answered:* forward is away from the kingdom's back rank (§3).
7. **Q7** *Answered:* the Chaturaji 2027 pieces do not join the catalogue, for now (§4).
8. **Q8** *Answered:* a step along a palace line (§5).
9. **Q9** *Answered:* confinement, to the palace the piece is in (§5).
10. **Q10** *Answered:* which die faces name which class (§6.1).
11. **Q11** *Answered:* how the game ends, and how it is won (§6.1).
12. **Q12** *Answered:* the four-throne bonus, once for each set of leaders (§6.2).
13. **Q13** *Answered:* promotion by a piece's own condition is free, optional and may wait (§6.2).
14. **Q14** *For playtesting:* the Chaturaji 2027 bonuses, ×10 to start with, and the leader bonus of 60 (§6.3).
15. **Q15** *Answered:* the flying-general capture (§6.4).
16. **Q16** *Answered:* the leader face-off (§6.4).
17. **Q17** *Answered:* castling, only with a Western Rook (§6.4).
18. **Q18** *Answered:* castling, "across check" without check (§6.4).
19. **Q19** *Answered:* a piece with a restriction lifted stays itself, with more moves (§7).
20. **Q20** *Answered:* the catalogue's own questions ([pieces.md](pieces.md#still-open)).
21. **Q21** *Answered:* a bound piece's value, by the share of the board it can reach (§7).
22. **Q22** *Answered:* the Makruk Pawn promotes to the Courtier; the Private is gone.
23. **Q23** *Answered:* a palace that does not fit its kingdom (§5).
24. **Q24** *Answered:* which way a kingdom lies in its corner is a setting (§2).
25. **Q25** *Answered (2026-10-29):* every piece whose normal move is forward only may make the
    back-to-back swap, with any other such piece (§6.1).
26. **Q26** Saved settings: versions, or a check for duplicates? Not important yet; the designer's
    thoughts are noted (§2.1).
