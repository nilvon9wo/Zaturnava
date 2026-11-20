# Ratings: Glicko-2 and TrueSkill compared, and a design for Zaturnava

*Proposed* (asked 2026-11-20). This is a design for review, not a description of what runs today. Nothing here is
built yet; the user asked for the design and its tradeoffs first. What runs today is in
[architecture](architecture.md): an Elo for each person at each game, from the team's result against what the
teams' ratings led to expect, and an Elo for each bot fitted from bot-only tournaments (`scripts/rate-bots.py`).

Throughout, **established** means part of the published algorithm, used as published; **approximation** means
a standard, documented adaptation with known costs; **extension** means a new model inspired by the algorithm,
which would need its own validation.

## 1. What the data looks like

Every rated game today is four seats in two teams of two, the teams fixed for the game alone. A result is a team
result: one team wins, or the game is drawn (agreed, or by a safety net ending on points). Each seat also scores
its own points (captures, bonuses), which are recorded but which the rules do not make the objective: a team
wins by its leaders and thrones, not by points.

Three facts shape everything below:

1. **The evidence is about teams.** A win says the team outplayed the other team. It says something about each
   member, but not the same thing about both, and not much about either on its own.
2. **Grand Legions games are rarely alike.** Board, kingdoms, rivers, armies, budgets, promotion, dice: the
   combinations outnumber the games anyone will play. Few configurations will ever have many games.
3. **There are few games altogether.** A rating system here must give useful answers from tens of games per
   person, not thousands.

## 2. The two algorithms, as published

**Glicko-2** (Glickman, 2001; *established*). Each player has a rating μ, a deviation φ (how unsure the rating
is) and a volatility σ (how erratic their results are). Games are grouped into rating periods; at the end of a
period each player's μ and φ are updated from their results against opponents treated as fixed, and φ grows
between periods with time away. It is defined for **two players, one result** (win, loss, draw). It has no
teams, no more than two sides, and no notion of a configuration.

**TrueSkill** (Herbrich, Minka, Graepel, 2006; *established*). Each player has a Gaussian belief over skill,
N(μ, σ²). In a game each player's performance is their skill plus noise of variance β²; a **team's performance
is the sum of its members' performances**; teams are ranked by performance, with a draw margin ε. The update is
approximate Bayesian inference (expectation propagation on a factor graph), done game by game, with a dynamics
term τ² added to σ² before each game so that skill may drift. Teams of any size, any number of teams, ranks and
draws are all part of the published model. **TrueSkill 2** (Minka, Cleven, Zaykov, 2018) adds per-player
experience effects, squad (repeated-partner) effects, quit penalties and per-mode offsets; TrueSkill Through Time
(Dangauthier et al., 2007) smooths the whole history instead of filtering forward.

## 3. Four seats, two temporary teams

### Glicko-2

There is no established way to rate a 2-vs-2 game with Glicko-2. The approximations in use, and what they cost:

| Approximation | How | Statistical consequence |
|---|---|---|
| Composite opponent | Each player plays one game against the opposing team's mean (μ, and a φ combined from both) | Ignores the partner entirely: a player carried by a strong ally gains as if they had won alone |
| Composite team | Each team is one player of mean μ and combined φ; the team's change is split between members | The split is arbitrary (equal, or by φ); the partner's strength enters only through the mean |
| Pairwise | Each player plays one game against each opponent (and none against the ally) | Each game is counted twice per player, so φ shrinks as if from twice the evidence: overconfidence unless the results are down-weighted, by hand |

Each makes Glicko-2 work, but each is an approximation, and the partner, the defining feature of these games,
is handled by a rule chosen by hand, not by the model. Doing better (weighting each player's update by how much
the team's result depended on them, given both partners' φ) means writing the Gaussian team model down
explicitly, which is TrueSkill's model in all but name: an *extension*.

### TrueSkill

The team factor is the published model. Team performance = performance of player A + performance of player B;
the update moves each member in proportion to their own uncertainty (σ²) relative to the whole game's
(c² = Σσ² + 4β²). So:

- **A team result is not equal evidence about both members** (§4.4 of the request). A newcomer (large σ) beside
  an established player (small σ) moves a lot; the established player moves little. This falls out of the
  algebra; no rule chooses it.
- **Teammate uncertainty enters the expectation** (§4.5): the win probability is Φ((μ₁ − μ₂) / c), and c grows
  with every player's σ, so a game with an unknown partner is predicted less confidently.
- **Temporary teams are not entities** (§4.1): only individuals are rated; a team exists for one game's factor
  graph and is gone.
- **Repeated partnerships** (§4.6): the base model has no partnership effect. TrueSkill 2's squad offsets
  are the published *extension*; with our volumes, two people rarely play together often enough to estimate
  one. Record partnerships; do not model them yet.
- **Individual contribution** (§4.7): the model credits each member only through the team's result. Points
  scored are tempting as an individual signal, but they reward a style (capturing) rather than winning, and a
  player who protects a leader or holds a throne scores little while deciding the game. Using them would
  introduce exactly the misleading individual measurement the request warns against. *Proposed*: do not use
  them for skill; keep showing them as their own statistic.

**Verdict on teams:** TrueSkill is a genuinely better starting point. Its advantage is not sophistication; it is
that the structure of our games (summed contributions, two sides, draws) is its published model, where
Glicko-2 needs a hand-chosen approximation.

## 4. Chance: dice, and armies

Both systems put all luck into one noise term: Glicko-2 implicitly, TrueSkill explicitly as β (how much a
single game's result varies around skill). That is the natural place for dice.

- **Dice versus no dice.** A dice game has more noise per game, so each result should move ratings less. In
  TrueSkill this is a **per-format β**: β_dice > β_no-dice, the same skill variable, and a result simply
  carries less weight where luck carries more. This is a small *extension* (β per game rather than per
  system), well within the model: β is already a free parameter, and per-mode noise is part of TrueSkill 2.
  Glicko-2 has no such parameter; the nearest approximation is scaling the g(φ) factor by hand.
- **Whether dice and no-dice should be separate ratings.** Skill at a dice game includes managing risk and
  odds; skill without dice is pure calculation. They are related but not the same. *Proposed*: **one skill per
  person per game family, plus a learned dice offset** (§6), not two pools. Two pools would halve the data in
  each and lose the strong relationship between them; an offset lets the data say how different they are.
  If, after enough games, offsets are large and uncorrelated with the base skill, splitting is justified then.

**Army valuation error** is chance of another kind: a deal may hand one side the stronger army despite equal
budgets. See §7; in the rating model it is, again, noise or an explicit team-strength term.

## 5. Configurations: record everything, rate little

Grand Legions has board size, kingdoms, rivers (layout and course), army method (A–D), open or secret drafting,
budget, promotion at the right edge, and dice. *Proposed* treatment:

| Variable | Separate pool? | Influences skill estimate? | Record? |
|---|---|---|---|
| Game family (C2027, GL) | **Yes** | – | Yes |
| Dice / no dice | No (offset, §6) | Yes: β per format, and a skill offset | Yes |
| Board and kingdom size | No | Later, as an offset by size class (small, medium, large, other) | Yes |
| Rivers | No | No (until data shows otherwise) | Yes |
| Army method (A–D) | No | Yes: β per method (identical armies less noisy than different ones) | Yes |
| Drafting open / secret | No | No | Yes |
| Budget, and points unspent | No | No (see §7.7) | Yes |
| Promotion at the right edge | No | No | Yes |
| Bots at the table | No | Through the bots' own ratings | Yes |

"Yes" under *record* means it is recoverable from the game's stored first action already, and should also be
written as columns for querying (§9).

## 6. Configuration-dependent performance without separate ratings

The request asks for one useful Grand Legions rating that still knows a player may be better at some
configurations than others. *Proposed*: a **hierarchical skill**:

    skill(player, configuration) = base(player) + Σ offset(player, factor level)

with factors few and coarse: **dice** (with / without) and, later, **board class** (small, medium, large,
other). Each offset has prior N(0, s²) with s small (say a quarter of β): an offset is believed to be zero
until a player's games say otherwise, and it never dominates the base.

In TrueSkill this is an *extension*, but a clean one: each player's performance in a game is the sum of several
Gaussian variables instead of one, which the factor graph handles without new mathematics (it is the same
summing as a team). Each game updates the base and the one or two offsets that apply, each in proportion to its
own uncertainty: a player new to dice games has an uncertain dice offset, so their first dice results move that
offset more than their base. This is exactly "experienced in one configuration, inexperienced in another" without
confusing inexperience with weakness: the offset's σ is large, not its μ low.

The **displayed rating** is the base (μ − 3σ, §10). Configuration offsets are shown on the player's card only
once they are both large and confident.

In Glicko-2 the equivalent would require its own derivation; the published update has nowhere to put an offset.

## 7. Army valuation and competitive balance

The draft values exist to make armies equal, not to handicap. The rating system should therefore **not** treat a
higher nominal value as a stronger army. Taking the questions in turn:

1. **Should nominal army value affect expected outcomes?** No. Within a game, budgets are equal by
   construction (methods A–D); a difference would only come from points left unspent in a draft, which is the
   drafter's choice and part of their play.
2. **Should actual composition be in the model?** Not in the rating model. Composition belongs to a separate
   model of **piece strength** (below), whose output may later feed the ratings as a correction.
3. **Can results estimate pieces' effective strength?** Yes, with care. Fit, over all Grand Legions games,
   P(team A wins) = logistic(skill difference + Σ_pieces w_p · (count of p in A − count in B)), skills taken from
   the rating system as fixed. The w_p estimate how much each piece is over- or under-valued *relative to its
   draft value*. Bot games supply most of the data: thousands are cheap to play, and bot skill is known.
   Self-play is already how the bots are rated; the same games serve here. This informs the *piece values*
   (docs/grand/pieces.md), which is where a correction belongs.
4. **Can it learn combinations?** In principle (pairwise interaction terms), but the number of pairs is large
   and data per pair small. Not before thousands of games per board class; until then, the per-piece residuals
   are the realistic target.
5. **Should army strength be modelled apart from skill?** Yes: kept apart, so that a player who favours an
   unusual army is not rated by their army. An unusual army is a choice whose value the piece model judges; the
   player's rating judges how they play.
6. **Identical versus different armies.** With identical armies (A, C), army strength cancels between teams.
   With different ones (B, D), it does not, and each game carries extra noise from valuation error. *Proposed*:
   β per method, larger for D (random different armies) than for B (each drafted, a skill in itself) and for
   A and C (identical).
7. **Unused budget as points.** Points decide nothing about the winner (§1), so they do not enter the expected
   outcome. A drafter who saves budget trades army strength for points; that trade is part of their play.

## 8. New players, and experience elsewhere

Four states must be kept distinct (the request's §9):

| State | Representation |
|---|---|
| No information | No row: the player is unrated at this game. The prior is N(μ₀, σ₀²). Display "unrated", never μ₀ |
| Estimate with large uncertainty | Row with σ near σ₀. Display a range, or "provisional" |
| Estimate from a related configuration | Base established, offset for this configuration still at its prior: the offset's σ is large, its μ zero |
| Established in this configuration | Base and the relevant offsets both with small σ |

**Transfer between families.** C2027 and Grand Legions stay separate pools; neither rating is copied to the
other. *Proposed*: a newcomer's Grand Legions prior may start at a mean shifted modestly toward their C2027 μ
(say a third of the way) **with the full σ₀**: an informed guess that the first few games quickly override.
Whether there is any correlation to exploit is an empirical question: measure it once both pools have players
in common, and set the shift to zero if there is none. Chess experience outside Zaturnava is unknown to the
server and is not used.

**Experience without rating.** Games played is a separate statistic (as "experience" is now), never folded into
μ: lack of experience is not low skill, only high σ.

## 9. Data model

Historical records stay immutable: every game's actions are already kept, its first action recording how it
was set up, so any future algorithm can recompute every rating from scratch. What is proposed adds
**queryable copies** of what the first action holds, and the ratings themselves, never replacing the record.

    GameResult            (one per finished game; written once, never changed)
      GameId              → the game's stored record (actions, seats)
      Family              C2027 | GL
      FinishedAt
      Outcome             winning team | draw; how it ended (leaders, agreement, safety net)
      HasDice             bool
      BoardWidth, BoardHeight, KingdomWidth, KingdomDepth
      Rivers, RiverCourse
      ArmyMethod          predefined | drafted | random-alike | random-each
      DraftSecrecy        open | secret | secret-from-allies   (null if not drafted)
      Budget              per army
      PromotesAtSide      bool
      CatalogueEdition    the pieces' edition the game used

    GameSeat              (four per game)
      GameId, Seat, Team
      PlayerId | BotId    who played it, as rated (a person's own AI marked, and not rated, as now)
      PointsScored
      ArmyValueSpent      nominal value of the army fielded
      UnspentBudget

    GameSeatPiece         (the army each seat started with)
      GameId, Seat, PieceKind, Count

    PieceValue            (versioned: values change; old games keep theirs)
      PieceKind, CatalogueEdition, Value

    Rating                (current belief; recomputable from GameResult)
      PlayerOrBot, Family, Factor (base | dice | board-class:…), Level
      Mu, Sigma, GamesCounted, UpdatedAt, AlgorithmVersion

    RatingHistory         (optional: each game's before and after, for charts and audits)

`AlgorithmVersion` makes recomputation safe: a new version is rebuilt from `GameResult` beside the old, compared,
and switched to, without touching any game.

## 10. Showing uncertainty

- **Leaderboard order**: by a conservative estimate, μ − 3σ (TrueSkill's own convention), so a newcomer with
  two lucky wins does not top the board. Show it scaled to the familiar Elo range (1500 ± …) so that today's
  numbers keep their meaning.
- **Next to it**: the range (μ ± 2σ) as a bar, not a sentence (figures go in tables), and "provisional" while σ
  is above a threshold.
- **Never** show μ alone for a provisional player, and never show σ as a "rating": a wide range means "not
  known yet", which must not read as "weak".

## 11. Side by side

| Criterion | Glicko-2 | TrueSkill |
|---|---|---|
| Four-player team games | Approximation (composite or pairwise); each with known bias | **Established**: teams and ranks are the model |
| Temporary teams | Handled by the approximation's split rule | **Established**: only individuals rated; teams per game |
| Individual skill from team results | By a hand-chosen split | **Established**: split by each member's σ² |
| Rating uncertainty | **Established**: φ, growing with time away | **Established**: σ, growing by τ² per game; time-based growth an easy extension |
| Volatility | **Established** (σ volatility): detects erratic players | Not in the base model (TrueSkill 2 has experience effects instead) |
| Configuration-dependent performance | Extension needing its own derivation | Extension within the model: extra summed Gaussian offsets |
| Army asymmetry | Noise only | Noise per method (β), or an explicit team-strength term |
| Dice | No natural place | β per format: small extension |
| New players | Prior (1500, 350) | Prior (μ₀, σ₀); transfer via shifted μ, full σ |
| Computational cost | Trivial | Trivial at our scale (a handful of Gaussian updates per game) |
| Implementation | Simple formulas, but the team adaptation is ours to invent and validate | Factor graph for 2 teams × 2 players is small and closed-form; well-documented open implementations exist to check against |
| Interpretability | Familiar to chess players | μ − 3σ needs a line of explanation; can be shown on the Elo scale |
| Long-term scalability | Weak: every new structure (more seats, 1-vs-1, offsets) needs new approximations | Strong: 1-vs-1, 2-vs-2, free-for-all and offsets are all the same graph |

Where Glicko-2 genuinely wins: **volatility**, and familiarity. Neither outweighs having the team structure right.

## 12. Recommendation

1. **Algorithm**: TrueSkill (base model, published), with three small extensions, each switchable and
   validated on held-out games before use: β per format (dice, army method); σ growth with time away, as in
   Glicko; and, once there is data, a dice offset per player (§6). Board-class offsets only later.
2. **Families**: C2027 and Grand Legions are separate pools. No automatic transfer; at most a modest, measured
   prior shift.
3. **Dice**: one Grand Legions pool, dice as a per-game noise level and a per-player offset; split only if the
   data shows the offsets large and uncorrelated with base skill.
4. **Record**: every variable of §5 and §9, as columns derived from the immutable game record.
5. **Influence on skill**: dice and army method (through β), dice offset (later), board class (later). Nothing
   else until the data shows it matters.
6. **Army valuation**: outside the rating model, as a piece-strength regression over all games, bots' included,
   feeding the draft values, not the ratings.
7. **Teams**: individuals only; teams per game; partnerships recorded, not modelled.
8. **New players**: unrated until their first game; prior N(μ₀, σ₀²); provisional while σ is high.
9. **Display**: μ − 3σ on the Elo scale, the range as a bar, "provisional" marked; experience as its own
   statistic.
10. **Evolution**: ratings recomputable from the record by algorithm version; the bots, whose skill is fixed
    and whose games are cheap, serve to calibrate β, τ and the offsets, and to test any change before people's
    ratings move.

**Bots** fit this design directly: a bot is a player whose skill does not change, so it is rated in the same pool
(as the people's leaderboard already counts a bot's team by the bot's rating), with τ = 0.

## 13. Validation before switching

A rating system is a predictor; it should be judged as one. Before replacing the current Elo:

- Replay every finished game in time order under each candidate (current Elo, Glicko-2 composite, TrueSkill,
  TrueSkill with extensions) and score **each game's prediction before it is played** (log-loss, Brier score).
- The candidate that predicts best on held-out games wins; ties go to the simplest.
- Bot tournaments give a second check: the bots' true order is known from thousands of games.

## 14. Open for the user

The questions this design leaves (whether it is accepted, the display scale, and whether past games are rated
again) are in [open-questions](open-questions.md#ratings).
