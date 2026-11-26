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
| Turn order | Sage looks through the dice of its own turn only; Tactician one round on | `Foresight` |
| Command against ownership | Thrones and leaders weigh it only in part | backlog §2, "Sage, next" |
| Promotion choice | By the promoted piece's value, not its safety or owner | `Appraisal` |
| Endgame technique | None beyond the points search; nothing pulls a piece towards a target | — |
| Swaps and other powers | Legal moves; they win no points, so Sage's search sees no gain in them | `GrandLegionsRules` |
| Ending by agreement | Bots accept or refuse a proposal; none proposes | — |

## The plan, step by step

Each step is small, keeps each bot's character (the weakest bots keep their randomness, §14), and is proved by a
test position in `tests/Bots/` (the strategic scenarios of §15) before its weight is tuned by self-play.

1. **Whose move is next.** Count a capture as safe or not by who moves before the bot again, ally or enemy, and
   whose command the piece is under. Test: the Yellow Rook case above.
2. **Promotion with eyes open.** Judge a promotion by the promoted piece's safety on its cell, its owner, and the
   command it brings. Test: the Pawn promoted into an attacked Raja. *The test position played by Sage*
   (2026-11-26, `StrategicPositionsTest`): a Black Pawn on its far edge may become Green's Raja or Black's own
   Elephant, both held by Red. With d8 attacked by Red's Elephant, Sage does not bring the Raja back there; with d8
   safe, it does, an ally's leader back on the board. Sage already weighs it, through its exposure: a piece put
   where it would be taken counts as lost. The bots that judge a promotion by the piece's value alone (Midas's
   greed, and those weaker) still may, as their character allows. Still to do: the promoted piece's command (whose
   pieces it brings back under whose control), which exposure does not see.
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
   it waits 12 turns (three rounds) before proposing again (*Proposed*). Per-bot thresholds can follow.
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
any pieces. *Tested* (2026-11-26, `StrategicPositionsTest`): two Elephants (the Rooks) and a Raja against a lone
Raja on Chaturaji 2027's board, without dice, Sage playing every side, the lone Raja fleeing as well as Sage can: it
is taken, and Black's team wins, within 120 decisions. Still to do: the ratings, before and after, to see the
stronger bots finish their won endgames in real games, with dice.
