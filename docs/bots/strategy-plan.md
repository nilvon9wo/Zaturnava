# Strategic play: what the bots can do, and the plan (2026-11-25)

The designer's specification "Strategic Intelligence and Bot Competence" (2026-11-25, §1–16) asks for bots that
play as a team, know whose turn and whose command it is, promote with purpose, finish won endgames, and agree to
end decided games, while each keeps its own character. This page maps what the bots do now against it, and lays
out the work in small steps, each with a test position that shows it done. The steps are in the
[backlog](../backlog.md#2-bots-that-play-well); this page is their reasoning. The earlier
[AI assessment](ai-assessment.md) covers how Sage and Athena are built.

## What was seen (2026-11-25)

| Seen in play | What the specification calls it |
|---|---|
| A Yellow Rook, moved by Red's Raja, takes Black's Rook, then is lost: Red ignored that Yellow and Black move before Red again | Command against ownership; turn order (§2, §3) |
| A piece sails to an empty corner, with no target and no reason | Purpose in the endgame (§5) |
| A bot walls in its own Raja | Coordination; the leader's safety (§4, §6) |
| A Pawn promoted into a Raja under attack, taken at once | Promotion judged by safety and command (§4) |
| No ladder mate with two Rooks against a lone Raja | Converting a won endgame (§5) |
| Back-to-back swaps never used, nor other special powers | Knowing the pieces' powers (§7) |

## What they do now

| Ability | Today | Where |
|---|---|---|
| Material, score, leaders, prisoners, thrones | Counted, for the bot's team against the other | `Appraisal.cs` |
| Threats and escapes | Counted; since 2026-11-25 escapes by the moves a piece may make, not its attacks | `Appraisal.Threats` |
| Allies | Their pieces count as ours; their turn is not foreseen except by Tactician | `Appraisal`, `Tactician` |
| Turn order | Sage looks through the dice of its own turn only; Tactician one round on; exposure by when each army moves, its commanders' turns included | `Foresight`, `Careful.Exposure` |
| Command against ownership | Thrones and leaders weigh it only in part | backlog §2, "Sage, next" |
| Promotion choice | By the promoted piece's value, not its safety or owner | `Appraisal` |
| Endgame technique | None beyond the points search; nothing pulls a piece towards a target | — |
| Swaps and other powers | Legal moves; they win no points, so Sage's search sees no gain in them | `GrandLegionsRules` |
| Ending by agreement | Bots accept or refuse a proposal; none proposes | — |

## The plan, step by step

Each step is small, keeps each bot's character (the weakest bots keep their randomness, §14), and is proved by a
test position in `tests/Bots/` (the strategic scenarios of §15) before its weight is tuned by self-play.

1. **Whose move is next.** Count a capture as safe or not by who moves before the bot again, ally or enemy, and
   whose command the piece is under. Test: the Yellow Rook case above. *Begun* (2026-11-26): the exposure every
   bot from Careful up weighs (`Careful.Exposure`) already let an army that moves before its attacker save its
   dearest piece; but it took each army to move on its own seat's turn only. Now an army moves at the first turn of
   any seat of its side that commands it (`IRulesApplying.MoversOf`: its own, a Raja on its throne, an ally with a
   Raja while it has none), so an enemy army commanded by an enemy who moves first threatens at once
   (`StrategicPositionsTest`: Yellow's Elephant, commanded by Red, who moves before Black). The chances the bot sees
   (`Appraisal.Threats`) likewise (2026-11-26): an enemy piece an ally attacks, whose army moves after that ally's,
   is taken before it can flee, and counts in full; Sage against Argus, 260 games each way, won 65% so, 63% before
   (within the noise: no worse). Still to do, with step 2: the promoted piece's army and its turn.
2. **Promotion with eyes open.** Judge a promotion by the promoted piece's safety on its cell, its owner, and the
   command it brings. Test: the Pawn promoted into an attacked Raja. *The test position played by Sage*
   (2026-11-26, `StrategicPositionsTest`): a Black Pawn on its far edge may become Green's Raja or Black's own
   Elephant, both held by Red. With d8 attacked by Red's Elephant, Sage does not bring the Raja back there; with d8
   safe, it does, an ally's leader back on the board. Sage already weighs it, through its exposure: a piece put
   where it would be taken counts as lost. The bots that judge a promotion by the piece's value alone (Midas's
   greed, and those weaker) still may, as their character allows. Still to do: the promoted piece's command (whose
   pieces it brings back under whose control), which exposure does not see; and turn order (*Stated* 2026-11-26:
   "smarter bots won't just consider whether it is safe to promote, but turn order: it might be advantageous for
   black to promote to green if it can rely on green attacking or defending something before black would get its
   next move"; and, the same day, "the point isn't just about what might defend the promoted piece, but how the
   promoted self itself might also be mobilized faster"). A piece promoted into an ally's army moves on that ally's
   turn: if the ally moves before Black's next turn, the promoted piece acts sooner than one of Black's own would,
   to attack, defend, flee or take, besides what the ally's other pieces may do for it. Exposure takes the enemy to
   move next as if at once, and a promotion's worth as if the piece waited for Black. Both are step 1's question,
   whose move is next, asked of the promoted piece and of the army it joins: to come with step 1, and only for the
   stronger bots. *Turn order, done* (2026-11-26, with step 1): a promoted piece is judged in the position after
   the promotion like any other, its exposure and its chances by when its own army moves next
   (`Careful.ArmyTurnsOf`), so for every promotion alike (*Stated* the same day): the bot's own Pawn or an ally's,
   into its own piece, an ally's, or, by a throne it holds, an enemy army's. *The test position played by Sage*
   (`StrategicPositionsTest`): Black's Pawn on c8 may become Green's Elephant or its own; Red's Horse forks c8 and
   Black's Raja, so Black spends its move on the Raja; Sage promotes into Green's, which moves before Red does, at
   every seed tried. Found on the way: promoting is not the bot's move, so a piece promoted into its own army may
   still act this turn, which Sage already counts; where it may, as when the new Elephant can take its attacker at
   once, promoting into its own is better, and Sage does. *The command a promotion brings, the F2 case played by
   Sage* (2026-11-26, `StrategicPositionsTest`): Black's Pawn on d8 may become Green's Raja, taking Green's Horse
   and Boat back from Black's command, or Black's own Elephant, which takes Red's Raja down the open file that same
   turn; Sage promotes into the Elephant, its look through its own turn seeing the capture. Still to do: the choice
   on command alone, with nothing to take. With Red's Raja off the file, Sage does not promote yet, moving Green's
   Horse first. Which is better there (an ally's leader back, or another army kept under one's own command) is for
   the ratings to show, not for guessing: the "commanded" measure, already offered, weighs nothing yet in Chaturaji
   2027.
3. **Pieces with a purpose.** In an endgame, pull each piece towards the enemy leader or its throne, and score the
   enemy leader's room to move (fewer cells, better). Test: two Rooks against a lone Raja reach mate. *Begun*
   (2026-11-25, with the endgame search below): the leader's room to move, and the leader attacked, are scored.
4. **Room for one's own leader.** Score the leader's own free cells, so that walling it in costs. Test: the trapped
   Raja. *Begun* (2026-11-25): a new measure, "leader-room", counts the moves a team's leaders have to cells no
   enemy attacks; its own leaders' room, and its allies', each weigh +0.05 a cell (*Proposed*) for the bots that
   weigh measures (Sage, Tactician, Narcissus, Epimetheus); the enemy's room is already pressed in the endgame.
   A Raja in its corner, walled in by its own Pawns, has none; in the open it has its three. Still to do: the
   trapped-Raja position played by Sage, and the weight tuned by the overnight ratings.
5. **Powers seen.** Value a swap by what it opens: a Pawn nearer its promotion, a Lance freed to strike a
   high-value piece, a file for a Rook. Each bot judges whether and when to swap as its strength allows (*Stated*
   2026-11-25). Test: two Pawns back to back, a Lance behind one with a Raja beyond. *Begun* (2026-11-25): a
   new measure, "held-back", counts a team's Pawns and Lances that an ally's piece holds back with no empty cell
   forward (a capture it could make does not count; the Shogi Knight, leaping past, is never held). Each piece
   held weighs −0.25 (*Proposed*) for the bots that weigh measures (Sage, Tactician, Narcissus, Epimetheus) and
   nothing for the weaker ones, which swap as they happen to. A swap that frees two Pawns gains 0.5; the ratings
   will tune that. What a freed Lance then attacks, the bots already count as chances. *The test position played
   by Sage* (2026-11-25, `StrategicPositionsTest`): it would not swap, though it judged the swap half a point
   better. Every bot, Random included, had been barred from any move onto its own team's piece, so as never to
   take one, and a swap is just such a move. Now only a move that leaves that piece off the board is barred, so
   the bots swap. Also found: in an endgame, Sage and Tactician look past their own turn, where a gain now and the
   same gain a turn later lead to one position; they picked between them at random, and so put gains off. Among
   actions worth the same looking ahead, they now take the one worth most at once (*Proposed*).
6. **Proposing an end.** Each bot, by its own thresholds of points and material, proposes to end a game it has won
   or lost, and keeps playing if the people do not agree (asked 2026-11-25). Test: a won position, the bot
   proposes; a refused proposal, it moves on. *Done* (2026-11-25, `Ending.cs`), every bot alike for now: it proposes,
   playing on, when its team leads by a clear lead on points and on the board, or trails past hope; once refused,
   it waits 12 turns (three rounds) before proposing again (*Proposed*). *Per bot* (2026-11-26, `Temper`,
   *Proposed*): Sage and Tactician propose ending a won game at three quarters of the clear lead; Random only at
   twice it, and calls a game hopeless only past twice its hope; every other bot as before. *The enemy's hope*
   (*Stated* 2026-11-26): whatever the lead, no bot proposes ending a won game while the enemy team, but its retired
   seats, has a leader, a heavy piece (Chaturaji's Elephant; Grand Legions' heavies), or a Pawn within two steps of
   its promotion (`IRulesApplying.StepsToPromotion`). *By team* (*Stated* the same day): a refusal makes the whole
   team wait the 12 turns, not just the bot refused.

   | Chaturaji 2027 (clear lead 10) | Points lead | Board lead | Enemy keeps | Realist | Even | Dogged |
   |---|---|---|---|---|---|---|
   | An Elephant, a Horse, two Pawns taken | 9 | 7 | Rajas | — | — | — |
   | Most of both enemy armies taken | 25 | 22 | A Raja | — | — | — |
   | The same | 25 | 22 | Two Boats, Pawns far back | Proposes | Proposes | Proposes |
   | Less taken | 13 | 9 | A Horse | Proposes | — | — |
7. **Measuring.** Each step's test position passes, and the overnight ratings do not fall; a step that lowers a
   bot's rating is undone or reweighted.

## Endgame tables

Asked (2026-11-25): could something like chess's endgame tablebases help? Not as chess has them: four armies, dice
that choose which piece moves, and armies dealt from a large catalogue make the number of positions far too large
to list. Small, exact help is within reach: for a few pieces left on the board, an expectimax search over the dice
to the end of the game, and patterns learned for the common endings (two Rooks, Rook and leader, against a lone
leader), used by the stronger bots.

*Asked for* (2026-11-25: "Please do this then"). *Begun* the same day (`Endgame.cs`), every number *Proposed*,
for the overnight ratings to tune:

| Part | What it does | Who |
|---|---|---|
| An endgame | One piece or fewer on the board for every 8 cells (8 pieces on a 64-cell board) | Every bot that weighs a position as Sage does |
| Looking further | Past its own turn, through the others' and the dice, up to 16,000 positions | Sage (in 5 seconds at most), Tactician |
| Pressing the leader | Each cell an enemy leader could flee to that the team does not attack: −0.25 of a point of material; the leader attacked: +0.5 | Sage, Tactician, Narcissus, Epimetheus; Athena and Pythia, as far as Sage's judgement counts in theirs |

The pressing is the ladder mate's pattern: no table of positions, but the measure it comes from, for any board and
any pieces.

**Corrected by the user (2026-11-27):** with few pieces left, the heavy pieces come first, not the leaders. "Eliminating
in on enemy rooks or other heavies should generally take priority over eliminating enemy leaders since eliminating the
leaders doesn't necessarily end the game and _most_ of them have limited mobility (but watch out for the exceptions,
especially the Dragon King)." What a leader threatens is the throne: "The most dangerous thing about the enemy leaders
in the end game (or at any time, really) is their ability to take the throne and take control of opposing pieces."
So, to do, measured bot against bot as each lands:

| Part | Now | To be |
|---|---|---|
| What the endgame presses | Each enemy leader's escapes | Every enemy piece, the more it is worth and the further it can move, the more (a Rook before a Raja; a Dragon King as the heavy it is) |
| A leader near a throne | Only an enemy leader beside the bot's own empty throne | Any enemy leader that could reach a throne of the bot's team, at any stage |
| A piece that could become a leader | Not seen | A piece that could promote into a leader near a throne; one sitting on a throne of the team, the more turns it has sat there (field promotion), the more urgent |

**What makes an escape** (the user, 2026-11-27): the count of a piece's open cells matters little, "it only just
needs one unguarded square"; what matters is whether there is time to reach it, and whether it is truly safe. And a
cell is not safe only because nothing attacks it now: "a square which is four steps away from a piece which only moves
one step at a time could be as little as one turn away if both the enemy and the ally are able to move it twice per
turn (of course, that involves some lucky dice rolls and the (im)probability should be weighed towards the safety of
the square)". So, by steps: first, no more than one escape counted, as likely as the dice let the piece move
(`Hunting.Escapes`, the challenger `tactician-cornering`); then each escape's safety a chance, from how many moves
each of the team's pieces needs to cover it, the dice giving one move or two, and who moves first, weighed towards
safety as doubles are rare. Distance, dice and turn order are played together, as one challenger, since apart they
mean little.

*Begun* (2026-11-27): `Hunting.OfHeavies`, played by challengers (`sage-hunting`, `tactician-hunting`) against the
bots as they are, on Chaturaji 2027 first.

**Testing faster** (the user, 2026-11-27: "fork the universe" at a decision; and "giving them different endgames - not
necessarily endgames they reached on their own - and see how well they do or don't finish, including whether they
maintain their leads or are self-sabotaging"). Built, in the arena and beside it:

| Way | What it saves |
|---|---|
| Each game played with the bots seated both ways, with the same dice; the pairs counted | Luck, which falls to each bot in turn |
| Games started where recorded games, people's as well as bots', came down to few pieces | The hour of play before an ending, alike for both |
| Each bot's finishing counted: leads kept, leads thrown away, games turned round | A rating of endgames alone, apart from the opening |
| Forks: only where two bots would choose differently, each choice played out many times with the same dice | Every decision where they agree, which tells nothing |

To come: endgames from Grand Legions games; endgames set by hand, as the user describes them (a leader two steps from
an ally's empty throne, the ally's last Rook on the board); and a bot's finishing shown beside its rating.

**A throne is worth what it controls** (the user, 2026-11-27, after guarding allies' thrones measured no better,
52 to 48): seizing a throne seldom matters, as it is hard to reach and by then there are few pieces left to control,
"but when it counts, it really can count", say when the enemy takes control of an ally's last Rook. And a test of
guarding "really depends upon whether the enemy has made it an objective to obtain the throne and can put it to good
use". So, to do:

| Step | Now | To be |
|---|---|---|
| A throne held | Counted as 1, whatever it controls | Counted by the worth of the pieces its owner still has |
| A throne threatened | Counted by the enemy leader's worth | Counted by what the ally would lose control of |
| The test | Against a bot that seldom aims for thrones | Also against a usurper, one that weighs thrones by what they control; and the games counted in which a throne was taken at all |

**Roles beyond the class** (the user, 2026-11-27): the UI gives a piece one class, but the bots may know it by more:
"Dragon Kings and M Chariots also functioning as heavies, or various other pieces also functioning as cavalry", and
roles the UI has no class for, "pieces which can do multiple captures in a single move or convert other pieces". To
do: roles read from how each piece moves and what its rules let it do, not tagged by hand (so a piece added to the
catalogue gets them too): heavy-like (slides along lines, whatever its class), cavalry-like (leaps), takes several at
once, converts. Each comes with a bot that uses it and a match that shows it helps; the first, heavy-like, so that
the hunt counts a Dragon King as the heavy it is, not only as a leader. *Tested* (2026-11-26, `StrategicPositionsTest`): two Elephants (the Rooks) and a Raja against a lone
Raja on Chaturaji 2027's board, without dice, Sage playing every side, the lone Raja fleeing as well as Sage can: it
is taken, and Black's team wins, within 120 decisions. Still to do: the ratings, before and after, to see the
stronger bots finish their won endgames in real games, with dice.

### Solved without dice? (asked 2026-11-27)

The user asked: leaving out the dice (so no double moves, no escapes) and ransoms, for how many pieces could an 8×8
game be solved, as chess's last pieces are? Counted for one set of pieces (which kinds, whose), each position a byte:

| Pieces | Positions (cells × seat to move) | Size of one table | Within reach here |
|---|---|---|---|
| 4 | 64⁴ × 4 ≈ 67 million | 67 MB | Yes: minutes |
| 5 | 64⁵ × 4 ≈ 4.3 billion | 4.3 GB | Barely: hours, a set at a time |
| 6 | 64⁶ × 4 ≈ 275 billion | 275 GB | No |

And there is a table for every set: 5 kinds, 4 owners. What it is worth:

- A deliberate move is open at every turn, so a win without dice is a plan the team can always follow. But it is not
  a sure win: the enemies may still roll for two moves, or an escape.
- Games do come down to few pieces. The user (2026-11-27): every game they know of was, in the end, one of
  attrition. The 132 finished Chaturaji 2027 games on the server that day (people's and the bots' own) bear it out:

  | Pieces left at the end | Games | Share |
  |---|---|---|
  | 4 or fewer | 15 | 11% |
  | 5 or fewer | 29 | 22% |
  | 8 or fewer | 74 | 56% |
  | 16 or fewer | 119 | 90% |
  | At most one team still led | 124 | 94% |

  So endings matter more than first thought here, and `Endgame`'s search, at 8 pieces or fewer, is in play in over
  half of all games.
- `Endgame` already searches endings of up to 8 pieces exactly, over the dice as they really fall, which a table
  without dice cannot do.

So the tables wait behind the search and judgement work, which also serves the endings: a 4-piece table (minutes to
build) may come next, as a test of `Endgame` and as its exact answer where it applies.

### When a bot accepts an end (the user, 2026-11-27)

A bot asked to accept defeat agrees today once it trails by more than it could hope to take back (`Hope`: each of its
pieces' best reachable enemy piece within a few rounds). The user's answer, on a Grand Legions game:

- **"Far behind" is relative to the game**: "In a large GL game, taking a leader and a few heavies already makes a
  difference of 100+ points, but that's far from unrecoverable. It needs to be considered how many points of material
  are actually in play, including the possibility that pieces may be ransomed or escape and that other bonuses are
  available." `Hope` counts none of ransoms, escapes or bonuses, and no absolute lead (`ClearLead`) should decide.
- **Two reasons to refuse, both valid**: the bot's own army mostly whole; its team keeping a leader, a heavy piece, or
  a Pawn about to promote. Bots of different tempers may weigh them differently.
- **The button is not the bots**: it shows how likely all would accept, so both reasons make it less green, whatever
  any bot's temper (done, PR #391).

*Done* (2026-11-27), for every bot: one whose own army is mostly whole (more than three quarters of its worth on the
board, against what enemies hold prisoner; Proposed) neither accepts nor proposes defeat, nor gives up its turns beside
a resting ally. Not played as challengers: a bot proposes an end only to an enemy with no leader, heavy piece or Pawn
about to promote, so in games between bots a whole army is never asked, and only people's games show it.

To do: hope measured against the points in play (on the board, held prisoner and ransomable, escapes, bonuses); the
second reason (a leader, a heavy piece or a Pawn about to promote) as some tempers' own, beside the points.

### Points or attrition (asked 2026-11-27)

- **How a game was won** (points, every enemy leader taken, a defeat proposed and agreed, out of steps), and the
  margin, are worth learning from. Athena's network already learns the margin beside the result. The next learner
  (Texel tuning: Sage's weights fitted to the outcomes of hundreds of thousands of recorded positions) will be fitted
  to the margin too, and its games will record how each ended.
- **Bots that prefer one way of winning** fit the design: the same search, with weights leaning to points (taking
  and cashing in) or to attrition (hunting leaders, keeping pieces). The draft could choose its army to match. They
  are not short-sighted, because the look ahead is the same. They come after the base bots stop blundering, as new
  personalities (bot design is ours to choose).
