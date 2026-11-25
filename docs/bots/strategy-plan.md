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
   command it brings. Test: the Pawn promoted into an attacked Raja.
3. **Pieces with a purpose.** In an endgame, pull each piece towards the enemy leader or its throne, and score the
   enemy leader's room to move (fewer cells, better). Test: two Rooks against a lone Raja reach mate.
4. **Room for one's own leader.** Score the leader's own free cells, so that walling it in costs. Test: the trapped
   Raja.
5. **Powers seen.** Value a swap by what it opens: a Pawn nearer its promotion, a Lance freed to strike a
   high-value piece, a file for a Rook. Each bot judges whether and when to swap as its strength allows (*Stated*
   2026-11-25). Test: two Pawns back to back, a Lance behind one with a Raja beyond.
6. **Proposing an end.** Each bot, by its own thresholds of points and material, proposes to end a game it has won
   or lost, and keeps playing if the people do not agree (asked 2026-11-25). Test: a won position, the bot
   proposes; a refused proposal, it moves on.
7. **Measuring.** Each step's test position passes, and the overnight ratings do not fall; a step that lowers a
   bot's rating is undone or reweighted.

## Endgame tables

Asked (2026-11-25): could something like chess's endgame tablebases help? Not as chess has them: four armies, dice
that choose which piece moves, and armies dealt from a large catalogue make the number of positions far too large
to list. Small, exact help is within reach: for a few pieces left on the board, an expectimax search over the dice
to the end of the game, and patterns learned for the common endings (two Rooks, Rook and leader, against a lone
leader), used by the stronger bots. That comes after step 3.
