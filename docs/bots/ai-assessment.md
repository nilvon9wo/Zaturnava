# AI assessment: Sage, Athena and Grand Legions (2026-11-11)

An assessment traced from the source, as the user asked for: what the bots do, what they cannot do, whether the
present training is worth going on with, and what architecture Grand Legions needs. No code was changed for it.

## 0. The short answer

- **Sage is a one-ply evaluator with a static-exchange patch, not a searcher.** It looks ahead only through the rest
  of its *own* turn. No enemy move is ever searched: what the enemy could do is guessed by a static exchange count
  of the position after Sage's turn.
- **In Grand Legions, Athena *is* Sage.** No network has been trained for any Grand Legions size, so Athena falls back
  to Sage's evaluator and weights, unchanged. The two ratings, which differ by about 6 Elo on each size, show it.
- **Neither bot learns from games played.** Sage's weights and Athena's one network (Chaturaji 2027 only) are files
  compiled into the assembly. They change only when someone runs the BotRunner offline and commits the result.
- **The weight tuning cannot work as built.** It runs about 6,000 games to test about 98 one-weight nudges, each at a
  power far too low for the size of effect a nudge has. The two changes it kept are about what chance alone would
  produce at its 2σ threshold.
- **The dice matter less than the design assumes, but they do matter.** At the start of every turn the player may
  make one deliberate move instead of rolling (rules.md §4). So each turn opens with a chess-like choice.
  - Once the player rolls, that choice is gone. The dice may forbid the move they would have made deliberately,
    but may give them a second move.
  - So a turn is a choice between a sure single move and a gamble on up to two class-restricted moves. The chance
    node sits inside that choice, not at the start of every turn as in backgammon.
- **Allies sit opposite** (rules.md §1), so the teams take turns alternately: A, B, A, B. But it is not simply a
  two-player game, for two reasons:
  1. **Allies are separate minds.** They may be unable to talk, or talk imperfectly. An ally may not play well, or
     as this bot would. Search cannot assume the ally plays the move the bot would choose for it.
  2. **Armies are mostly, but not always, separate.** Usually each seat moves only its own army. Through thrones
     and regencies (§6), seats sometimes command overlapping sets of pieces, so who can move what must come from
     the rules at each node, never be assumed.

  So search should treat the bot's own decisions as max nodes, the enemies' as min nodes, and the dice as chance
  nodes. The ally needs a model of its own (§5.3): a min node is too pessimistic, a max node too trusting.

## 1. What the current system actually does

### 1.1 Move selection: `Foresight` (shared by Sage and Athena)

[Foresight.cs](../../src/Bots/Zaturnava.Bots/Foresight.cs), with `Depth = 3` and `Beam = 3`
([SageBot.cs](../../src/Bots/Zaturnava.Bots/SageBot.cs)):

- `Choose` applies **every** playable action through the real rules engine (`rules.Apply`). It then scores the state
  that results with `WorthOf`.
- `WorthOf` acts on whose decision it is:
  - **Another seat's decision:** it returns the static value at once (`_valuing.ValueOf`). Apart from one exception
    below, this is where every line ends.
  - **Sage's own dice are awaited:** it averages over all 21 unordered 2d6 outcomes (`Outcomes`). That part is real
    expectimax, *over Sage's own roll only*.
  - **Still Sage's own decision:** `BestOf` takes the best of three things:
    - stopping where it is;
    - each next action, valued statically;
    - the top 3 of those actions (by static value), followed one step further.
- The exception is `WorstReplyOf`: when an enemy holding Sage's throne moves Sage's army (§6.3), it takes the minimum
  over that enemy's legal actions, one ply.

So:

- **A deliberate move is judged one ply deep.** After it, the turn passes, so the line ends at the static value.
- **A roll is judged as the average, over the dice, of the best one or two die moves (beam 3), each valued
  statically.**
- **No enemy reply, and no ally reply, is ever searched.** The class summary says so itself: "the enemy's reply
  weighed only as that counts it".

### 1.2 Position evaluation: `Appraisal` (Sage, and Athena outside Chaturaji 2027)

[Appraisal.cs](../../src/Bots/Zaturnava.Bots/Appraisal.cs) `ValueOf` is a weighted sum
([SageWeights.cs](../../src/Bots/Zaturnava.Bots/SageWeights.cs)). Each term other than Exposure and Chances is a
`Sided` tally: own seat, ally and enemies, each with its own weight. The terms:

| Term | What is computed |
|---|---|
| Score | Points per side. |
| Material | The sum of `WorthOf(kind)` of the pieces on the board. **A fixed worth per kind**, priced once per board size (`BoardPrices`). It is the same wherever the piece stands. |
| Leaders | A count of leaders on the board. |
| Prisoners | The worth of enemy pieces held. |
| Thrones | The number of enemy thrones that a side's leaders sit on. |
| Exposure | `Careful.Exposure`: the **single worst** loss any one allied piece could suffer in a static exchange, plus any threat to an empty own throne. |
| Chances | The **single best** static exchange the team could start on any enemy piece. |
| Mobility | The number of cells each side attacks. |
| Measures | The ruleset's `ArmyMeasures` ([ArmyMeasures.cs](../../src/Games/Chaturaji2027/Zaturnava.Games.Chaturaji2027/ArmyMeasures.cs)): **army-wide sums**. They are pawn progress, nearness to promotion, total move count ("reach"), backward moves, leaders attacked, thrones in danger, pieces commanded, and worth standing on escape squares. |

How the exchange terms work:

- `Careful.AttacksBy` collects `rules.AttacksOf` for every seat on a side and groups the attacks by target cell.
- `AttacksOf`
  ([Chaturaji2027Rules.cs:357](../../src/Games/Chaturaji2027/Zaturnava.Games.Chaturaji2027/Chaturaji2027Rules.cs#L357))
  lists each piece's direct attacks, filtered by terrain (`MayGo`).
- `Careful.Exchange` is a classic static exchange evaluation (SEE). The cheapest attacker takes, the cheapest
  defender retakes, and either side stops once going on would lose.

### 1.3 Athena

[AthenaBot.cs](../../src/Bots/Zaturnava.Bots/AthenaBot.cs):

- **Search:** the same `SageBot.Play`, so the same `Foresight`, depth and beam.
- **Evaluation**, from [NetworkValuing.cs](../../src/Bots/Zaturnava.Bots/Learning/NetworkValuing.cs):
  - the network's predicted outcome;
  - minus 0.05 × `Careful.Exposure`;
  - plus 0.002 × Sage's whole appraisal;
  - minus a small penalty for a non-leader parked on an enemy throne.
- **The network** ([ValueNetwork.cs](../../src/Bots/Zaturnava.Bots/Learning/ValueNetwork.cs)):
  - one hidden layer of 32 tanh units, plus direct linear weights from input to output;
  - two outputs: outcome (−1 to 1) and margin.
- **Its inputs** ([PositionEncoder.cs](../../src/Bots/Zaturnava.Bots/Learning/PositionEncoder.cs)):
  - per relative seat and per kind, a count of pieces and the **total** rows they have advanced;
  - prisoners, scores, whose decision it is, the dice, and who has resigned or retired;
  - Sage's features.
  - The shipped network sees the board summed up only: **it never sees where any individual piece stands.**
  - *Built and trained (2026-11-27):* `--view board` gives a network learned afresh the board square by square
    too, as each seat sees it from its own edge. For each square and each seat: what its piece there is worth, whether
    it leads, whether it is heavy, and how many of its pieces attack the square. Worth, not kind, so that Grand
    Legions' ever-different armies share what is learned. Networks learned before still fit.
    Three rounds, learned from the overnight games and its own (`scripts/athena-rounds.py --view board`), each 400
    games against Sage:

    | Round | Won | Lost | Share |
    |---|---|---|---|
    | 1 | 237 | 163 | 59.2% |
    | 2 | 279 | 121 | 69.8% |
    | 3 | 273 | 127 | 68.3% |
- **Training** ([Learning.cs](../../src/Bots/Zaturnava.BotRunner/Learning.cs)): offline TD(λ), with λ = 1 by
  default, which is plain Monte Carlo regression to the final result. It learns from recorded games of the existing
  bots.
- **Coverage:** [Networks/](../../src/Bots/Zaturnava.Bots/Networks) holds only `chaturaji-2027.json`. For every Grand
  Legions size, `AthenaBot.Valuing` returns null, so Athena uses `Appraiser` with Sage's learned weights. The only
  other difference, answering an end-of-game proposal by the network, also needs the network, so it falls back too.
- **Offered only where she has a network**: so far, Chaturaji 2027 (`AthenaBot.Knows`), as her profile says.

### 1.4 Sage's weight training

[Training.cs](../../src/Bots/Zaturnava.BotRunner/Training.cs) and
[SageMatch.cs](../../src/Bots/Zaturnava.BotRunner/SageMatch.cs) do coordinate ascent:

- For each weight in turn, multiply it by 1.3 (or set it to 0.3 if it is 0).
- Play a match of `--games` × 2 games, seats swapped, Sage-with-the-nudge against Sage-without. Draws and margins of
  0 are discarded.
- Keep the nudge if wins − losses > 2·√(wins + losses).
- Otherwise, if the weight is not 0, try × 0.7 the same way.
- `--train N` repeats this whole pass N times.

## 2. Can the evaluator see the tactics you listed?

Take the rook example: a red rook attacks a yellow boat and a yellow horse, and the boat defends the horse's square.

- Sage applies "rook takes horse" for real.
- In the resulting position, `Exposure` runs SEE on the rook's new square: the boat attacks it.
- Unguarded, the rook is counted lost; guarded by something cheaper than a boat, it costs rook − boat.
- So Sage weighs about "+horse +points − rook" against "+boat +points − (whatever else hangs)". **This simplest case
  it gets right**, as long as the defender attacks the square directly.

What it cannot do, traced to the code:

| Situation | What the code does |
|---|---|
| **Two or more pieces left hanging** | `Exposure` takes the **max** loss (`losses.Append(0).Max()`), not a sum. The max is a fair guess when the enemy has one move. It is wrong here, because before Sage moves again, *two* enemy seats each move, and either may roll for two moves. |
| **Forks the enemy can make next move** | Never seen. Nothing models an enemy move that *creates* attacks. Only attacks already standing are counted. |
| **Forks Sage makes** | `Chances` is the **max** over targets, so a fork scores the same as a single threat. It also assumes Sage's team moves next, but an enemy moves next. |
| **Pins and x-rays** | Grand Legions has no check, so leaders are simply taken and there are no absolute pins. Relative pins and x-rays are invisible: `AttacksOf` gives direct attacks only, so a battery (rook behind rook, say) counts one attacker, and a pinned defender still counts as a guard. |
| **Discovered attacks** | Seen only if Sage's own move uncovers one, because it is then present in the resulting static position. An enemy's discovered attack is never seen. |
| **Overloaded defenders** | Not seen. One defender is counted as guarding every square it attacks, all at once. |
| **Exchange order across four seats** | `Exchange` alternates "all enemies" against "all allies" immediately. It ignores that the attackers belong to different seats, which move at different times, with an ally's turn between them. |
| **Whether a valuable piece is safe to take** | Yes for a single, direct, one-square exchange, through SEE. No for anything that needs a reply and a counter-reply. |
| **Where a piece stands** | Material is fixed per kind. Position enters only through army-wide sums: total reach, total pawn progress, and so on. No term asks whether *this* knight is centralised, *this* leader is shielded, or *this* rook is trapped. Athena's encoder deliberately throws location away. |

**Does it evaluate a move's consequences, or compare features?** Both, but shallowly:

- It plays the move with the real engine, so the immediate consequences (captures, promotions, points, thrones) are
  exact.
- Everything after that is a feature comparison of the resulting position, with SEE standing in for the reply.

## 3. Is the training worth continuing? No.

### 3.1 The run you asked about (`--train 2 --games 30`, grand-legions-large)

**What `--train 2` means.** Two full coordinate-ascent passes over every weight. That build weighed 4 measures (the
output shows them), so there were 32 weights:

- 15 sided (5 terms × 3 sides);
- exposure and chances;
- 3 for mobility;
- 4 measures × 3 sides.

**The scheduled work.**

- Each nudge plays 2 × 30 = 60 games.
- Each pass makes 32 "up" tries, plus up to 17 "down" tries. Only non-zero weights get a down try, and mobility and
  the measures started at 0.
- That is up to about 49 matches, or ≈ 2,900 games, per pass: about **98 tests and ≈ 5,900 games** in all.

**Did it finish?** Yes. `artifacts/sage-grand-legions-large.json` is no longer empty. It holds 306 bytes, written
2026-11-09 20:55, and the background task reported exit code 0.

**Why the file was empty for days.** `SelfPlayCommand.Output` opens the file with `File.CreateText` at the start.
The weights are written only once, at the very end. Nothing intermediate was ever saved.

**Where progress went.** One line per nudge was written to standard error, in the background task's log. I did not
read that log: you told me earlier never to open those files. If you want the per-nudge record, it is the
`b2o2pr76c.output` task file.

**What it found.** Compared with the defaults, it changed only two weights:

- `chances` went from 0.3 to 0.21;
- `retreat.ally` went from 0 to 0.3.

### 3.2 Why "a coin flip or worse" is the expected result

- **False positives.** The acceptance test is a one-sided ~2σ test, which passes about 2.3% of the time when nothing
  changes. Over ≈ 98 tests that is about 2 false acceptances. Two is what was kept.
  - `retreat.ally`, a weight on how many backward moves the *ally* has, is a plausible false positive.
  - The same pattern appeared before: the medium weights gained `leaders.enemy` 2.6 and `retreat.ally` 0.3.
- **Too little power.** A 30% change to one weight among 32 shifts the win rate by perhaps a percent or two. To
  detect +2% reliably takes thousands of games per test, not 60.
- **A capped ceiling.** Even perfectly tuned, the result is a linear combination of aggregate features over a
  one-ply search. The features in §2 cannot express the tactics, and no weighting can add them.
- **Tuning only against itself.** Sage tunes against Sage, so it learns to exploit its own blind spots, not to play
  well.
- **Bad operations.** There is no ETA, no checkpoint, and no resumption. The output file is truncated at the start,
  so a crash loses everything.

**Recommendation:**

- Stop tuning this way.
- Do not commit the large weights it produced. They are within noise of the defaults.
- Keep Sage, as it is, as the baseline every new bot must beat.

## 4. What a tactically competent bot needs

1. **A real search over every seat's moves.** Value positions for the bot's team, zero-sum against the enemy
   team, on the win first, then the point margin.
   - **The bot's own decisions** are max nodes, and **the enemies'** are min nodes, searched with alpha-beta.
   - **The ally's decisions** are not the bot's to choose. Model them, don't maximise over them:
     - as the ally's likely move (for example the move a cheap policy such as Sage's would pick);
     - or as an expectation over its few best moves.
     How much to trust the ally can follow its rating, as backlog §Trusting the ally proposes.
   - **A roll** is one option at the start of a turn. Its node averages over the dice (*-minimax Star1/Star2 bounds
     let alpha-beta prune there too). Within it, only the moves the dice allow are generated, so the deliberate
     move is no longer on offer.
   - **Who may move which pieces** comes from the rules' legal actions at each node. Thrones and regencies sometimes
     let seats move pieces that are not their own.
2. **Quiescence search.** At the depth limit, keep searching captures (and promotions and throne entries) until the
   position is quiet. This, not a cleverer SEE, is what finds forks, overloads, x-ray recaptures and "taking the
   horse loses the rook". Order moves by MVV-LVA (most valuable victim, least valuable attacker).
3. **Search plumbing:**
   - iterative deepening under a time budget per move;
   - a transposition table with Zobrist hashing;
   - killer and history move ordering.
4. **A cheaper representation for search.** The engine's `GameState` uses immutable collections, and `Appraisal`
   rebuilds the attack maps several times per call: `Exposure` twice, `Reach` twice, and the measures again. The
   rate of roughly 1,500 games a day on 5 cores suggests the bots evaluate slowly. A searcher needs:
   - **First, a benchmark of nodes per second.** This is the one big unknown, and it decides how deep search can go
     on the free hosting.
   - Then either a lean mutable board with make/unmake, used only by the bots and checked against the engine by
     tests, or heavy caching in the engine.
5. **Handling the branching at chance nodes.**
   - **The size of the problem.** A roll has 21 outcomes, each with up to two die moves in either order. On the
     12×12 board, deliberate moves alone may number 100+.
   - **Bounding it:**
     - search roll nodes one ply shallower;
     - sample outcomes (for example 6–8, stratified by class) rather than enumerating all 21;
     - generate die moves per class in advance.
6. **An evaluation that knows where pieces stand.**
   - Per-piece terms: piece-square by class and board zone, leader shelter, piece safety (attacked and not defended
     enough), trapped pieces, promotion races.
   - Tune them with **Texel-style tuning**: logistic regression of the evaluation against outcomes over hundreds of
     thousands of recorded positions. That takes minutes, against days of match play, and has statistical power
     that per-nudge matches never will.
7. **Statistics that can tell bots apart.** Compare bots with SPRT (sequential probability ratio test), which stops
   as soon as a difference is clear, not with fixed 60-game matches. Run every new bot against the whole field, not
   only itself.

## 5. Recommended architecture for Grand Legions

### 5.1 The separation you suggested

Split move selection from position evaluation, behind interfaces that already half exist:

- an **evaluator** (`IPositionValuing` today);
- a **searcher** (`Foresight` today);
- a **bot**, which pairs one evaluator with one searcher and adds the non-board policies. Those are trading,
  ransoms, end proposals and chances, which stay rule-based for now.

Then every bot is a pairing, and the arena measures each pairing.

### 5.2 Bots to build, in order

| # | Bot | Search | Evaluation | Why |
|---|---|---|---|---|
| 0 | **Sage** (exists) | own turn only | linear, aggregate | Baseline. |
| 1 | **Tactician** (new) | alpha-beta + *-minimax + quiescence, iterative deepening | Sage's terms + per-piece safety, Texel-tuned | The biggest likely gain for the least risk. It fixes exactly the failures in §2. |
| 2 | **MCTS bot** (new) | MCTS with chance nodes, progressive widening; rollouts cut short by the evaluator | same evaluator | Anytime, and copes with the huge roll branching. It tests whether selective sampling beats full-width search here. |
| 3 | **CNN value net** (later) | Tactician's or MCTS's | small fully-convolutional net over board planes (piece-kind channels × owner relative to the bot), global pooling to the value | One net serves 6×6, 8×8 and 12×12. It learns location, unlike the present encoder. Train it on **search-labelled** positions (distillation), not raw game results. |
| 4 | **Hybrid** (later) | MCTS with a policy + value net (AlphaZero-like) | CNN | The strongest in principle, and the costliest to train. Worth it only if 3 beats 1 clearly. |

### 5.3 Trade-offs

- **Alpha-beta (Tactician)**
  - For: deterministic and debuggable; exact on tactics within its horizon; cheap with no training.
  - Against: weak where the branching is huge (rolls, 12×12) unless pruned well. The evaluation still caps
    strategy.
- **MCTS**
  - For: copes with stochastic, wide trees, and is anytime.
  - Against: plain MCTS is notoriously poor at sharp tactics (it misses narrow refutations) unless guided by a good
    evaluator or policy. In a capture-heavy game like this, it may well lose to alpha-beta at equal time.
- **CNN**
  - For: sees location and interactions; one net covers every board size.
  - Against: needs a lot of labelled positions and CPU time. Training and inference stay free on our own hardware,
    but slow. A small net (a few conv layers, ~50k parameters) can run in plain C#; ONNX Runtime is an option.
- **The ally.** Alpha-beta has to fix the ally's move from a model at each ally node. MCTS can simply play the ally
  by a policy in its simulations, which suits an ally who is not perfect.
  - Either way, the model of the ally is a choice to measure, not to assume.
  - Paranoid search (ally as min) and cooperative search (ally as max) are the two extremes to compare against.
- **Pure learning without search** (the MLP today, or a policy-only net): not recommended. Tactics need look-ahead.
  Every strong game AI of this kind (chess, shogi, Go) pairs learned evaluation with search.

### 5.4 Operational rules for any future long run

- Write the best result so far after every improvement, never only at the end.
- Report progress with counts and an ETA.
- Allow resuming from a checkpoint.
- Size a run in advance and say how long it will take before starting it.

## 6. Proposed next steps (2, 3 and 5 begun: see Progress)

1. **A benchmark:** nodes per second for `rules.LegalActions`, `rules.Apply` and `Appraisal.ValueOf` on each board
   size. It decides how ambitious step 2 can be.
2. **Tactician v1:** alpha-beta, quiescence and *-minimax over the engine as it is, with a test suite of tactical
   positions: the rook/boat/horse case, a fork, an overloaded defender, an x-ray recapture.
3. **The arena:** SPRT matches against Sage and Argus, and the rating script over the whole field.
4. **Texel tuning** of the evaluation from recorded games, replacing `--train`.
5. **Profile corrections:** fix Athena's description for rulesets where she has no network, now; for instance say
   that she plays as Sage there. Or train her a Grand Legions network only once a better encoder exists.
6. **MCTS and CNN bots,** after the Tactician shows what search alone is worth.

### Progress (2026-11-11)

- **Tactician v1 is built** (`TacticianBot`, `Tactics`). It is alpha-beta over every seat's decisions: the bot's
  own are max nodes and its enemies' min nodes. An ally is modelled as choosing for itself, one step ahead, rather
  than maximised for the bot. Dice nodes average over every outcome. It uses iterative deepening within a budget
  of positions, not of time, so it plays the same on any machine. It orders actions by Sage's judgement and follows
  the 6 most promising past the first step. A decision with one thing to do (passing at a turn's end, say) does not
  shorten the look. A look the budget cuts short keeps what it finished. Leaves are judged by Sage's `Appraisal`,
  whose exposure term stands in for quiescence for now.
- **The arena exists** as an on-demand test (`ArenaRun`, [self-play](self-play.md#rating)), since it needs no
  command beyond the test runner.
- **First results:** at Chaturaji 2027, Sage 30, Tactician 30, in 60 games. A budget of 4,000 positions reaches
  little more than Sage's own look, because each turn of the others brings its own 21 dice outcomes. To beat Sage
  it needs a cheaper evaluation, a transposition table, or sampling the dice deeper in the tree.
- **Step 5 is done:** Athena's profile says she plays as Sage at any game but Chaturaji 2027.

### Progress (2026-11-12)

- **Pythia, the MCTS bot (row 2), is built** (`PythiaBot`, `MonteCarlo`). It is open-loop UCT. Each of 1,000
  passes per decision rolls the dice afresh wherever the game waits on them, so the tree holds actions alone, and
  each way the dice may fall is sampled as often as it is likely. A pass stops at the first action not tried yet, or
  8 decisions deep, or at the game's end. It judges the position there by Athena's network (Sage's `Appraisal` where
  there is no network), with no random rollouts, as AlphaZero does. Its team maximises and its enemies minimise.
  Values are scaled to those seen so far, so the exploration constant (√2, *Proposed*) means the same at any game.
  It plays the action tried most. It is not yet the hybrid of row 4: it has no policy network to say which actions
  to try first, so untried actions are tried in random order. A CNN (row 3) is still to come.
- **Not yet rated:** its rating waits for an arena run, which needs the user's machine.
- **Threats now expect the reply** (the user's play of 2026-11-12). Sage's appraisal counted any enemy piece
  the team attacked as half-won, as if no enemy moved first. So a Boat would threaten a Horse that was about to take
  a stuck Pawn and leave, rather than guard the Pawn to take the Horse back. And it would chase a Raja it had
  forked rather than take the Pawn left behind. The appraisal now counts nothing for an enemy piece that could
  itself take one of the team's (it will take and be gone). Of each enemy's other threatened pieces, it counts only
  the second most it could lose, since the enemy saves the dearest (a fork) (*Proposed*). Athena's network inputs
  keep the old count, which it learned from. Sage's tuned weight for this term was tuned on the old count, so a
  re-tune may move it.
- **No trade pens a piece in** (the user's play of 2026-11-12). Green accepted an even trade that sent Black's Pawn
  home beside Green's Rook in Black's corner, trapping it. Every bot now refuses to offer or accept a trade that
  would leave any piece of its team with at most one cell to go to, where it had more (*Proposed*). The cells a
  piece attacks stand in for where it may go.
- **Exposure follows the turn order** (the user's play of 2026-11-12). Green declined its Rook back on its home
  cell, beside Red's Raja, although Green plays before Red and the Rook could take the Raja first. Exposure, which
  Careful, Argus, Sage and so Athena and Pythia's fallback all count, once let any enemy reply first. Now a seat of
  the team whose turn comes before an enemy's saves the piece it would lose most to that enemy, by moving it or
  taking its attacker, so only its second-worst such loss counts (*Proposed*). The turn is taken to go round the
  seats in their order, as both rulesets have it.
- **A Pawn is worth keeping** (the user's play of 2026-11-12). Green, commanding Black's army, moved Black's Rook
  off guarding Black's Pawn to take Yellow's Boat, and the Pawn was lost: the Boat scores 2 and the Pawn 1. But a
  Pawn may yet be promoted, and a Boat reaches few cells. A ruleset now says what each piece is worth keeping
  (`IRulesApplying.KeepingWorthOf`), apart from what taking it scores; Chaturaji 2027's, from its table
  `Values/chaturaji-2027-keeping.json`, counts a Pawn as a Horse, 3, and every other piece as it scores
  (*Proposed*). Exposure weighs the team's pieces, and every piece that might take or be taken in an exchange, so;
  what a taking scores is still its points. Grand Legions keeps its prices. A bot commanding a person's army moves
  its pieces as a person would (*Stated* 2026-11-12): no rule keeps it off them; a good player should be able to
  count on a good bot to move a piece when it should be moved and leave it where it is when it should be left,
  the piece, the position and everything else weighed. So the cleverer the bot, the better a partner.
- **A fork of two enemies is worth making** (the user's play of 2026-11-12). Green's Rook could fork Yellow's King
  and Red's Boat, but moved a Pawn instead: each enemy, moving before Green, was counted as saving its piece, so the
  fork counted for nothing. Now each enemy saves only the dearest of its threatened pieces that has an empty cell
  to flee to that the team does not attack; one cornered counts in full. And a piece saved still costs its enemy
  its turn, so it counts for a quarter of its worth besides, for every enemy that must flee (*Proposed*).
- **A Rook is not given for a Horse** (the user's games of 2026-11-27, replayed in `StrategicPositionsTest`). Sage
  took a Horse two Pawns guarded with its Rook, and the Rook was lost; and left a Pawn of its line unguarded to
  threaten a Pawn that could not move. Its learned weights counted a piece taken, in points and on the board, at 2.4
  a point, but a piece left to be taken next at 1.3. A loss next now weighs at least as much as it would cost once
  made: the enemy's points up and the team's pieces down by as much (*Proposed*). And Careful's exposure, which
  every bot from Careful up counts, took any retake for certain; now each counts only as likely as the dice let the
  retaker move (`IRulesApplying.MovesPerTurn`, at most certain; *Proposed*). And exposure counted only a seat's
  worst loss, so that a second piece left unguarded, while one hung already, cost nothing; Red, on two Rook dice,
  took both. Now the second-worst counts for half besides (*Proposed*: a turn's dice may let an enemy take twice).
  The user: "the bots do not adequately
  value position above points", the same fault as "backgammon in 2D" (backlog §1, real AI bots).
- **Every piece left to be taken counts** (2026-11-27, from the games played: `BlunderRun`). Replaying the server's
  132 finished Chaturaji 2027 games found each move whose piece an enemy took before the bot's next turn, its team
  2 points or more worse off by the end of that turn. Most such pieces stood attacked once the bot's turn was over
  (Sage 75 of 91, Tactician 46 of 55, Athena 76 of 103); the rest an enemy reached in two moves on its dice. Put to
  the bots as they play now, Sage, Tactician, Athena and Argus still made 77 of those losses. The cause: exposure
  counted only the team's worst loss and half the next, the worst seat's alone. So once one piece hung, a third,
  or an ally's, cost nothing, and Yellow's Raja walked into Black's camp to take a Pawn, in six games. Now each
  enemy army, in the order they move before the team again, takes the most it could from a piece it attacks, and
  half of its next (*Proposed*). The four bots' repeated losses fell from 77 to 46: Sage's from 27 to 13, its Raja's
  walks from 9 to 3. Athena repeats the most of those left (22), as her network does not see where the pieces
  stand, yet she still beat Sage 26 to 14 in 40 games (`ArenaRun`, after the change); her weighing waits for a network
  that sees the board.
- **A piece behind the taker** (2026-11-27, from `BlunderRun`). Sage's Yellow Horse went beside Green's Raja, guarded
  by Yellow's Rook; the Raja would not take, it counted, for the Rook would take it back. But Green's Rook stood
  behind the Raja, and would take back in turn; the Raja took. The exchange count saw only the pieces attacking the
  cell as the board stood. Now, for a piece of the team attacked and guarded, it sees too what the enemy's cheapest
  taker would open behind it, once it took (*Proposed*). Repeated losses: 46 to 45, Sage's 13 to 12; that game's
  Horse now stays.
- **Challengers: one change, the same bot on both sides** (2026-11-27, the user: "proof instead of speculation").
  A challenger (`Challenger`, in the BotRunner's tests) is one of our bots with a single setting changed, played
  against the bot as it is (`ArenaRun`, `"botAssembly": "self"`), each on both teams' seats. 100 games each:

  | Challenger | Changed | Won | Lost | Kept? |
  |---|---|---|---|---|
  | `sage-hunting` | Heavies hunted before leaders in an endgame; allies' empty thrones guarded | 52 | 48 | No: no measurable difference |
  | `athena-steady` | Sage's appraisal counting ten times as much beside her network (0.02, not 0.002) | 44 | 56 | No |
  | `tactician-hunting` | As `sage-hunting` (1 game drawn) | 43 | 56 | No: leaning worse; a heavy's many escapes may outweigh all else |
  | `tactician-guarding` | Allies' empty thrones guarded, and nothing else | 52 | 48 | No: no measurable difference |
  | `sage-usurping` | A throne held counted by what it controls (vs Sage) | 55 | 45 | No: within chance, and it seized no more thrones (13 games to Sage's 14) |
  | `sage-guarding` | Allies' empty thrones guarded by what is at stake (vs Sage) | 49 | 51 | No measurable difference |
  | `sage-guarding` | The same, against `sage-usurping` | 57 | 43 | Not yet: guarding did 12 games better against the usurper than Sage did, but the usurper seized more thrones against it (20 to 14), so the guard is not shown to be why |
  | `tactician-cornering` | Heavies hunted first, but at most one escape counted, as likely as the dice let it move | 57 | 43 | Not yet: promising (as hunting alone lost 43 to 56), to be confirmed in more games |
  | `tactician-cornering` | The same, from 74 recorded endgames, each played from both sides, each pair of games with the same dice | 74 | 74 | No: pairs 6 won, 6 lost, 62 even; thrones seized 23 each. The 57 to 43 was chance |
  | `sage-wary` | Forks an enemy could make on its next move counted, as a piece left to be taken is (vs Sage) | 99 | 101 | No: pairs 16 won, 17 lost, 67 even |

  Started from the same endgame with the same dice, 62 of 74 pairs ended alike: in an endgame of eight pieces or fewer
  the position and the dice decide most games, whichever way a bot hunts. A way of hunting shows only in the few pairs
  that differ, so endgame matches need many starts, or the forks (`ForkRun`), which play out only where bots differ.
  From the beginning too, with the same dice, two Sages that differ in one judgement end two thirds of their pairs
  alike: the dice decide much, and a small change in judgement shows in a third of the games at most.

  Thrones not a team's own were seized in 11 to 20 of each 100 games, by either side: more often than feared, so a
  difference in guarding them can show, given more games or paired ones.

  Before challengers, steadier Athena beat Sage 65 to 35, as Athena does (25 to 15 in 40); against Athena herself,
  she lost. Her network's swings are better left as they are than drowned in Sage's count.
- **Not done: a Pawn won back counted as its points.** Counting an enemy piece taken back as the points it scores,
  not as what it is worth keeping (a Pawn 3), changed nothing measurable (46 to 45, Sage's 13 to 14), and the case
  that suggested it was sound: Green's Rook took a Pawn that attacked its Raja. Left as it was.

Uncertain until measured: how fast the engine evaluates, and therefore how deep alpha-beta can reach on 12×12 within
a move's time. Step 1 settles it before anything is built on it.
