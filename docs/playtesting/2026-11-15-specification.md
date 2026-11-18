# Master playtesting, development and research specification (2026-11-15)

The user's consolidated playtesting notes, kept as they gave them: the authoritative working checklist,
subject to the decisions recorded elsewhere (docs/grand/specification.md, docs/grand/pieces.md,
docs/bots/). What has been done about each item is tracked in [traceability.md](traceability.md); this
file is not edited except to add notes the user gives later, dated.

Earlier notes the user gave the same day, before consolidating, are folded in below where they add
anything ("Notes before consolidating", at the end).

## Purpose

I've been extensively playtesting Zaturnava, particularly Grand Legion (GL), and have compiled a
substantial collection of bug reports, feature requests, design decisions, and future development ideas.

This document is the consolidated specification. It should be treated as the authoritative working
checklist, subject to the historical decisions and clarifications described below.

The objective is not simply to acknowledge the observations or propose solutions. Investigate the
existing implementation, identify root causes, implement appropriate corrections and enhancements, and
verify the results.

## A. General development instructions

### A1. Understand before changing

- Read and understand the existing implementation before making changes.
- Avoid introducing workarounds that conceal underlying problems.
- Preserve existing game rules and functionality unless a change is explicitly requested.
- Distinguish between bugs, missing functionality, design improvements, performance problems, and
  future roadmap items.
- Where an observation suggests a possible underlying cause, investigate the hypothesis rather than
  assuming it is correct.
- Prefer game-specific implementations and strategies over generic logic that makes assumptions about
  another game.
- Do not introduce unnecessary complexity or duplicate existing functionality.
- Pay particular attention to performance and correctness when different games' pieces are used
  together.
- Test changes against relevant games and configurations, not merely the original reported scenario.
- If a requirement is ambiguous, investigate the existing rules and implementation first. Ask me when a
  consequential design decision cannot reasonably be resolved from the available information.

### A2. Preserve project history and decisions

**IMPORTANT: Preserve the entire history of our discussions about Zaturnava, including decisions,
answers, explanations, and measurements of bot strengths.**

Do not retain just the latest conclusions or discard earlier information.

In particular, preserve:

- Actual bot strength measurements.
- Earlier bot evaluations and comparisons.
- Decisions about intended bot difficulty.
- Explanations of why particular bots have particular strengths.
- Previous balancing decisions, including decisions subsequently revised.
- Game-rule decisions and their rationale.
- Answers to questions and design discussions.

Do not replace actual measurements with approximate values or a newly invented rating scale.

Update persistent project documentation so these decisions remain available in future development
sessions.

Maintain appropriate documentation for game rules, piece definitions, bot descriptions, technical
implementation, and design decisions.

### A3. Implementation discipline

For each requirement:

1. Examine the existing implementation.
2. Identify affected components, rules, algorithms, and dependencies.
3. Establish the underlying cause of any reported bug.
4. Implement the appropriate solution.
5. Add or update automated tests wherever practical.
6. Test relevant edge cases and interactions with other game rules.
7. Update documentation.
8. Record the implementation status.

Do not mark an item complete merely because related code has changed or compilation succeeds.

If the entire scope is too large for one session, divide it into manageable phases. Complete and verify
each phase before proceeding, maintaining a clear record of outstanding work.

## B. Game mechanics and piece behavior

### B1. Infantry promotion configuration

Introduce a game-configuration option determining whether infantry reaching the far right edge of the
board can be promoted.

- Default: disabled.
- When disabled, infantry cannot promote merely by reaching that edge.
- When enabled, the existing promotion mechanics should apply.

### B2. Back-to-back movement

Back-to-back is unreliable.

Observed problem:

- Black is on d2.
- Green is on d3.
- Black cannot move its pawn from d2 to d3.

Investigate the implementation and correct the underlying problem.

Test other relevant positions, directions, and colors.

### B3. General (Janggi)

General (Janggi) is not functioning correctly.

Observed problem:

- A yellow Threeleaper captured the black General (Janggi).
- Yellow received no points.
- The black General was not made available for ransom.

The Janggi General should behave like an ordinary capturable piece in this respect.

**Important distinction:** Only King (Tafl) should have the special ability to be removed from the game
through an escape, rather than ordinary capture. This distinction must not accidentally be applied to
General (Janggi).

### B4. Elephant (Janggi)

Observed position:

- Elephant on f2.
- River lies between f2 and g2.
- The elephant cannot move anywhere to its right.

Investigate and correct its movement rules, including interaction with the river.

### B5. Elephant (Xiangqi)

Elephant (Xiangqi) does not function correctly when initially positioned on the far side of a river.

Observed problem:

- In a 26×26 board game with 23×3 kingdoms, elephants positioned to the right of the river could only
  move if they jumped over the river and landed on the left side.
- They could not move around on the right side.

Movement restrictions must be defined relative to the actual river, not assumptions about which quarter
of the board or kingdom contains the elephant.

Test elephants on both sides of the river, including nonstandard board and kingdom dimensions.

### B6. Guard illustration

The Guard illustration is confusing.

- It suggests that the Guard can move diagonally in general.
- In reality, it can only move diagonally along palace lines.
- Depending on the location of the throne within the palace, the diagonal shown in the illustration may
  be misleading.

Correct the illustration to accurately represent legal movement possibilities, taking palace and throne
orientation into account.

### B7. King (Tafl) and Swede capture rules

King (Tafl) and Swedes should be able to use allies, as well as other pieces under their player's
control, to participate in captures.

Additionally, they become **berserk** when their throne is controlled by an enemy.

A berserk piece behaves as follows:

- Whenever maneuvered into a position where it becomes the third piece in an uninterrupted orthogonal
  line (row or column), it captures the piece in the middle.
- The color or allegiance of the piece on the other side is irrelevant.
- The piece in the middle can be friendly or hostile.
- A berserk piece can capture friendly and hostile pieces concurrently when the conditions are met.

Implement this consistently for both King (Tafl) and Swedes.

Test:

- Ordinary captures.
- Captures using allies.
- Berserk activation and deactivation.
- Captures of friendly pieces.
- Captures of hostile pieces.
- Simultaneous captures involving friendly and hostile pieces.

## C. Piece cards and illustrations

The illustrations are becoming increasingly confusing, particularly for:

- Checkers pieces.
- Tafl pieces.
- Pieces whose movement or capture depends on the palace or river.
- Pieces whose movement can be blocked.

The current approach of attempting to show everything simultaneously is not working.

### C1. Show only relevant symbols

Do not show symbols in a piece's legend when they are not relevant.

Examples:

- War Engine only leaps, so it does not need a general "moves" symbol.
- War Engine does not need a forward-direction symbol because its orientation is irrelevant.

Review all piece legends for similar unnecessary symbols.

### C2. Interactive illustration controls

Introduce controls on the piece card rather than attempting to show every possible situation
simultaneously.

At minimum:

- Toggle between illustrating movement and illustrating capture.
- For relevant pieces, toggle whether the illustration accounts for a palace or river.
- For pieces whose movement can be blocked, provide another toggle for illustrating blocked movement.

Blocked-movement illustrations should work as follows:

- Introduce other pieces onto the board to illustrate the obstruction.
- Show the resulting invalid movement possibilities being removed.
- Do not rely on red X symbols to communicate everything.

King (Tafl) and Swede capture illustrations must show the additional piece necessary to complete the
capture.

### C3. Animation

Ideally, illustrations should be animated to demonstrate:

1. The initial position.
2. The movement of the piece.
3. The resulting position.
4. The removal of captured pieces.

Animation is desirable but must not come at the expense of accuracy or comprehensibility.

### C4. Responsive layout

Readers must not have to scroll horizontally to see piece cards.

Review card dimensions, content layout, and responsive behavior.

## D. Bots: game knowledge, strength, and performance

### D1. Bots must understand the games they play

Some bot descriptions suggest that bots do not actually know how to play particular games and instead
fall back on the logic of another bot.

That is not useful.

If a bot does not know how to play a particular game, it should not be offered as an option for that
game.

Review:

- Bot capabilities.
- Game-specific evaluation logic.
- Move generation and evaluation.
- Bot descriptions and advertised capabilities.
- Assumptions that cause logic from one game to leak into another.

Do not allow a bot to appear capable of playing a game when it is actually using inappropriate logic.

### D2. Weakest bots are still too strong

The non-random weakest bots are still too strong.

According to previously provided strength measurements, Icarus is only a few points below Midas.

There should be **at least three non-random bots rated below 1200**.

Use actual historical bot strength measurements and previous discussions when rebalancing bots.

The objective is a meaningful progression of difficulty, including genuinely accessible weaker
opponents, rather than several bots clustered together at similar strengths.

### D3. Bot performance and timeouts

Some bots have become unacceptably slow, even in C2027, particularly when checker pieces are present.

Swedes may also be contributing to performance problems.

Sometimes the smartest bots appear to become permanently stuck evaluating their turn.

Implement a timeout mechanism of approximately 30 seconds.

When the timeout expires, the bot should make the best move it has managed to evaluate so far rather
than remaining stuck indefinitely.

Investigate these hypotheses:

**Hypothesis 1:** The algorithm for checking multiple jumps is fundamentally inefficient or defective.

**Hypothesis 2:** Because game-specific bot logic has not been implemented adequately, GL-bot logic is
leaking into C2027 and evaluating conditions that are impossible or irrelevant in that game.

Investigate both hypotheses without assuming either is correct.

The solution should:

- Prevent indefinite evaluation.
- Preserve the best move found so far.
- Avoid unnecessary computation for rules and conditions that cannot apply to the current game.
- Keep bot behavior correct across different games and piece combinations.

### D4. Game-specific Grand Legion bots

Create relatively strong bots for Grand Legion, each with a strong preference for pieces originating
from a particular game.

There should be at least one bot for every noteworthy game from which we have adopted pieces:

- Western Chess.
- Xiangqi.
- Janggi.
- Shogi.
- Makruk.
- Checkers.
- Tafl.
- Fairy.

The Fairy bot should specifically prefer pieces that are NOT used by any of the other listed games.

If a game-specific bot requires pieces that are not currently available:

- Determine whether the missing piece should be added.
- Otherwise, identify and use the closest available equivalent.

These bots should have meaningful preferences for their respective piece families while remaining
competent Grand Legion players.

## E. Army selection and composition

The algorithm for selecting armies is still inadequate and needs substantial improvement.

### E1. Default army composition

Unless a bot has a specific strategic reason to prefer another composition, its default objective
should be:

- At least one leader per 20 pieces.
- Approximately 50% infantry.
- Remaining pieces divided more or less evenly between heavies, cavalry, and artillery.

### E2. Default budget and occupancy

The default budget should assume that at least 80% of the squares in the kingdom, including 80% of the
squares in the palace, will be occupied.

A player may choose to leave spaces open for maneuverability.

There should be no special preference for occupying the palace with pieces that are bound to the
palace.

If palace-bound pieces are commonly selected in excessive numbers, there is probably something wrong
with the piece-selection algorithm.

### E3. Piece cost and selection frequency

There should be an inverse relationship between the cost of a piece and the frequency with which it is
selected.

In general:

- Expensive pieces should be uncommon.
- Extremely inexpensive pieces should also be uncommon unless there is a strategic reason to select
  them in large numbers.
- Examples of valid strategic exceptions include creating a mob of cheap infantry or cheap cavalry.

Consider both weighted averages and averages that exclude outliers when evaluating overall army
composition and piece costs.

Avoid simplistic selection algorithms that repeatedly choose the cheapest available pieces or exhaust
the budget on expensive pieces.

### E4. Multiple selection strategies

Bots may have multiple army-selection strategies.

- They do not need to have multiple strategies.
- Strategies may be shared between bots.
- Different bots may have different strategic preferences.

More intelligent bots may also respond to the pieces other players are selecting or placing.

This can be implemented as part of the selection strategy itself or as an override of the normal
strategy.

### E5. Custom game budget

When selecting either:

- "Every army at random", or
- "One army for everyone"

in a custom game, the user must still be able to specify the points available to spend.

Random army selection must not eliminate control over the budget.

## F. Grand Legion promotion and strategic decision-making

Promotion decisions are particularly important in Grand Legion because pieces of different colors and
allegiances can have consequences extending across multiple players' turns.

### F1. Promotion scenario: Green Raja and black pawn

Observed situation:

- Green Raja is on its throne at e8.
- A black pawn is on e7.
- Green Raja does not move, leaving the pawn unable to promote or even escape attackers.
- Eventually I get to promote the piece to a rook.
- Green then uses that rook to capture a Raja, sacrificing the rook to the pawn that was protecting the
  Raja.

Investigate whether the bot understands the consequences of leaving the pawn blocked and the strategic
consequences of its subsequent sacrifice.

### F2. Promotion scenario: choosing the wrong color

In another situation:

- Green gets to promote a black pawn.
- It should promote to a green rook, providing mobility and allowing Green to continue coordinating
  both green and black pieces across both our turns.
- Instead, it promotes to a black Raja, slowing movement and disrupting coordination.

Promotion evaluation must consider the identity and allegiance of the promoted piece, its mobility, and
its effects on coordination.

### F3. Promotion scenario: failure to pursue a winning endgame

A third observed situation:

- Black has Raja and little else.
- Black Raja is sitting on Red's throne.
- Red has only Raja left (probably should have retired, but never mind).
- Black is keeping Red safely in the corner.
- As Red Raja is still alive, there is no risk of Red Raja taking Black Raja while he is on the throne.
- Green has very few pieces left (a few pawns, a boat, maybe a horse).
- Yellow has nothing left.
- Green pawn reaches the end where it can promote.
- It should have promoted to a rook to easily kill Red Raja and win the game.
- Instead, the pawn promotes to Green Raja, so I lose control of Green pieces.
- Green Raja meanders towards/around Black throne.
- Yellow Raja escapes.
- Instead of heading towards Yellow throne, where it might have taken control of Yellow, Green Raja
  continues to meander around Black throne.
- Yellow Raja heads to and takes Green throne and now has partial control of Green pieces, which it
  uses against me and uses to kick me off the Red throne.

This is an important strategic failure involving:

- Promotion choice.
- Preservation of control over pieces.
- Recognition of winning opportunities.
- Movement toward strategically relevant thrones.
- Failure to anticipate an opponent's opportunity to seize control.

The bot must evaluate the strategic consequences of its choices across the entire multiplayer position,
not just the immediate value of an individual piece.

### F4. General promotion evaluation

Investigate bot decision-making around:

- Protecting and coordinating pieces.
- Promoting pawns.
- Choosing the appropriate color and type of promoted piece.
- Evaluating mobility and strategic consequences.
- Avoiding unnecessary sacrifices.
- Recognizing which throne is strategically relevant.
- Recognizing when moving a leader or other piece is necessary to unblock a pawn.
- Prioritizing a reachable enemy throne over remaining near an allied throne.
- Preserving or acquiring control of other players' pieces.
- Anticipating the consequences of throne occupation and loss of control.
- Recognizing and exploiting opportunities to take control of another player's pieces through throne
  occupation.

Other observed endgame problems:

- Failure to make optimal use of rooks.
- Failure to push pawns toward promotion.
- Excessive dithering instead of pursuing an achievable victory.
- Turning winning positions into losing positions.

Improve endgame evaluation and decision-making.

Bots should recognize and prioritize:

- Immediate threats to their thrones and leaders.
- Opportunities to remove an enemy Raja from a throne.
- Opportunities to secure a win or prevent an opponent from winning.
- Rook mobility and effective rook usage.
- Pawn advancement and promotion opportunities.
- The urgency of converting a winning position into a victory.

The bot should not continue playing as though it were in an ordinary opening or middlegame when the
position demands decisive action.

### F6. Recognizing hopeless endgames and resignation

Smarter bots should also recognize when an endgame has become hopeless.

For example:

- A GL game is being played on a large board.
- One side is ahead by 500 points.
- That side has many rapidly moving pieces.
- The opposing side has been reduced to slow-moving infantry.
- Those infantry are far from any realistic opportunity for promotion.
- Most cannot leave their palace.

In such a situation, the disadvantaged side might reasonably throw in the towel rather than continue
losing a war of attrition.

Different bots could have different thresholds for deciding when a position is hopeless.

Investigate an appropriate approach to recognizing hopeless positions and deciding when to resign. This
should be a strategic decision rather than a simplistic points comparison.

## G. Evangelist

I have added another piece to the spreadsheet: **Evangelist**.

Inspect its actual spreadsheet definition and establish its complete movement rules and special
ability.

Its inability to capture does NOT mean it is weak.

Its special ability is extremely powerful and should result in a very high piece cost. Its effect is
arguably much worse than ordinary capture.

Do not invent, simplify, or substitute an interpretation of its ability.

Implement the actual rules defined in the spreadsheet.

Ensure that Evangelist is integrated into all relevant systems:

- Movement and special-ability evaluation.
- Capture and interaction rules, where applicable.
- Army selection.
- Piece pricing.
- Bot evaluation.
- Piece cards and illustrations.
- Other systems dependent on piece definitions.

## H. Lobby and game creation

### H1. Lobby layout

Reorganize the lobby:

1. Move the Games list panel to the rightmost position instead of the New Game panel.
2. If a player has games waiting for them, display those games ABOVE the general list of games, not
   beside it.
3. If there are games the player could join, hide the New Game panel by default.
4. Add a New Game button at the top of the Games list. Clicking it should reveal the New Game panel.

### H2. Shareable games

When a player creates a game with open seats, provide a link that can be shared with other people so
they can join.

Likewise, when a player joins an existing game that still has open seats, provide a shareable link.

The link should take recipients directly to the relevant game/joining process.

### H3. Game configuration validation

When creating a new game, validate kingdom dimensions.

Show an error if:

- Kingdom dimensions exceed board dimensions.
- Kingdom dimensions make it impossible to generate a valid game in which kingdoms do not overlap.

Perform this validation before attempting to generate an invalid game.

### H4. Games list presentation

The current list takes up too much space and is visually cluttered.

Use tables instead of long sentences to present structured information.

Requirements:

- Each game must occupy exactly one row.
- Include a column indicating who is expected to play next.
- Include how long that player has been waiting.
- Make columns sortable by their headers.
- Default sorting should prioritize games waiting for the current user, followed by games that have
  been waiting the longest.

Keep the presentation compact and readable.

## I. Placement previews

When placing palaces, thrones, pieces, and other objects, provide a visual preview of the intended
placement.

This applies both during initial setup and during play.

When the mouse hovers over a square or squares that can be clicked for placement:

- Display the actual piece or object that would tentatively be placed there.
- Position it exactly where it would appear.
- Make it desaturated.
- Make it blink to communicate that it is only a preview and has not actually been placed.

This applies to objects including palaces and thrones, not just ordinary pieces.

The purpose is to make placement more intuitive and visually clear without confusing tentative
placement with actual game state.

## J. Escape, hiring, ransom, and promotion interfaces

### J1. Escape and hiring

When there are pieces available to escape or hire, do not present enormous yellow buttons.

The interface already has too many large yellow buttons, which look cluttered and confusing and do not
establish a clear next action.

Instead, list the available pieces in a manner similar to how pieces are already presented when
available for ransom.

### J2. Piece information

When piece cards are enabled, clicking a piece's symbol should display information about that piece.

This should allow the player to make an informed decision about what to do and, where applicable, where
to place the piece.

Apply this behavior to:

- Escaping pieces.
- Pieces available for hire.
- Pieces available for ransom.

### J3. Promotion choices

When promoting a piece, the player should be able to click beside each promotion option to learn about
the piece they could promote to.

Make it possible to inspect available promotion choices before committing to one.

## K. Loading performance and progress indication

When visiting Itch, the interface appears to load incrementally.

Initially, only a minimal amount of the interface is displayed. The remaining interface, including
important and interesting information displays, takes some time to appear.

Improve this experience.

Requirements:

- Show a spinner and/or progress indicator so users understand that the application is loading.
- Where possible, communicate progress or give some indication of how long loading may take.
- Investigate whether portions of the application can be deferred or loaded asynchronously.
- Prioritize making the application interactive and displaying useful information as early as
  possible.
- Avoid unnecessarily delaying the initial usable experience while waiting for less essential
  components.

## L. Bot dictionary button

The separate "The bots" dictionary button is unnecessary.

Users can already click a bot's name in either the leaderboard or scoreboard to learn about that bot.

Remove the redundant button while retaining the existing ability to access bot information through bot
names.

## M. Automated bot tournaments, training, and ELO

I want a simple, documented, offline way to run bot tournaments locally, completely automatically and
independently of Claude CLI executing or monitoring the tournaments.

The system should support two objectives:

1. Training bots which use AI.
2. Ensuring all bots have ELO scores.

### M1. Independent execution

I should be able to run tournaments without Claude CLI needing to remain active or supervise the
process.

These do not necessarily need to be separate processes.

Provide simple documented commands to initiate and manage the process.

### M2. Incremental persistence and interruption recovery

Tournament results must be saved incrementally.

The process must tolerate being shut down arbitrarily, potentially with little warning and without
graceful termination.

It should be possible to resume without losing substantial previously completed work.

Persist sufficient information to recover tournament progress, training progress, and scoring results.

### M3. AI learning

Ideally, AI bots should be able to learn from observing successful play.

Investigate suitable training approaches and how successful game outcomes can be used to improve bot
behavior.

### M4. Results and repository integration

Either the raw results, or the results after running a second documented command, should become part
of the repository and, through it, part of the data available to Itch.

Investigate whether the tournament system could safely use the same database as Itch.

Theoretically, it should not matter whether games are played through Itch or locally, provided the
database remains consistent and valid.

However, this could become problematic if or when the games are radically different.

Database sharing is an option to investigate, not an unconditional requirement. Avoid risking
corruption of the live database.

### M5. ELO coverage

All bots should have ELO scores.

The system should make it possible to compare bots and track the effects of training and balancing over
time.

Preserve historical measurements rather than overwriting the evidence used for earlier decisions.

## N. Future roadmap: secondary palaces (OQ7)

**Priority: Low. Do not implement yet. Include in the development roadmap.**

The game is not ready for this feature.

However, as already implemented:

1. Mercenaries can only be placed in palaces.
2. We have agreed that it will eventually be possible to purchase promotions and "lifts" in palaces.

Having more space for pieces, or even more palaces, could therefore become valuable, particularly in
large games.

### N1. Multiple-palace configuration

- It should make sense, particularly for large games, for players to have or be able to obtain more
  palaces.
- Whether multiple palaces are possible should be a game-configuration option.
- Multiple palaces should not be introduced until players can also purchase promotions and lifts.

### N2. Settling and establishing a secondary palace

Once a game is in progress, pieces will be able to "settle", in which case they will be consumed.

**Settling and creating secondary palaces**

- A palace occupies a fixed 3×3 area on the checkerboard grid.
- To establish a new palace, the settling piece must occupy the centre square of the proposed 3×3
  palace footprint. The settling piece is consumed when it settles.
- The player chooses which of the nine squares in the new palace will contain its throne. The throne
  does not have to be in the centre.
- Before confirming settlement, the player must be able to see the proposed 3×3 palace footprint and
  choose the throne's position within it.
- The settlement interface should make the distinction between the palace footprint and the throne
  position clear. The footprint is determined by the settler's position; the throne position is a
  separate player choice.
- Local thrones should look distinctive and less impressive than the primary thrones.
- Settlement might cost points; whether it does so should be configurable.
- It remains undecided whether every piece can settle, only certain types of pieces can settle, or
  settlement requires dedicated Settler pieces.
- The proposed palace must meet the applicable placement constraints. The original idea was that it
  should be surrounded on all sides by empty squares, although allowing friendly pieces may be worth
  considering. Avoid introducing unnecessary configuration options.

Unresolved design questions:

- I am not sure whether every piece should be able to settle, only some pieces, or only dedicated
  "Settler" pieces (not yet detailed).
- To settle, a piece must be completely surrounded on all sides by empty squares, or possibly friendly
  pieces as well.
- Whether friendly pieces are allowed could be configurable, although I am wary of making too many
  things configurable.
- The player should be able to see where the local throne will be.
- Local thrones should look distinctive and "less impressive" than primary thrones.
- Settling might cost points. Whether this should be configurable is also unresolved.

Do not prematurely settle these design questions.

### N3. Recruiting pieces into secondary palaces

It might also be possible to recruit new pieces (not yet in the game) into a palace.

There might be a premium for recruiting pieces after initial game setup. For example, they might cost
double.

Whether this premium is configurable remains an open question.

### N4. Local throne control

All palace activities, other than mercenary placement, should depend on the local throne being
occupied.

If an allied or enemy Raja is on the throne in a secondary palace:

- They only have control over pieces in that palace.
- This control can be used to move pieces out of the palace, including choosing their destination.

### N5. Razing palaces

There might later be a concept of razing palaces, in which case secondary palaces might be promoted.

This is a future design possibility, not an implementation requirement at present.

## O. Research and writing: comprehensive article about AI in Zaturnava

I have already posted one article about this project on LinkedIn:

<https://www.linkedin.com/pulse/zaturnava-building-multiplayer-chess-variant-game-engine-kessler-5yhef/>

I would like to follow it with another article giving more information about what we have tried and
learned when applying AI to this project.

This is a research and writing deliverable, not simply another game feature.

### O1. Approaches already tried

Explain:

- What approaches we have used.
- Why we chose them.
- How well they worked.
- What did not work and why.
- What we learned from the experiments.

Base this on the actual project history, implementation, and previous discussions rather than inventing
a retrospective.

### O2. Alternative approaches

Investigate further approaches, including hybrid approaches we might try.

Consider their potential benefits, drawbacks, complexity, and suitability for this project.

### O3. Differences between C2027 and Grand Legion

Investigate whether significant differences are necessary between an AI designed for C2027 and one
designed for Grand Legion.

In particular, consider the complications introduced by:

- Larger armies.
- Less predictable army composition.
- Less symmetrical armies.
- Pieces with different capture methods.
- Pieces with special powers.

Discuss which aspects of AI can be shared and which require game-specific logic or evaluation.

### O4. Further AI research directions

Explore additional avenues for producing better bots.

Consider how the AI might incorporate different strategic paradigms, including:

- The positional tactics of chess.
- The randomness of backgammon.
- The negotiated endings of Go.

Investigate whether and how these different approaches could contribute to Zaturnava's AI.

### O5. Can Zaturnava be solved?

Explore whether Zaturnava is a game that could ever be solved.

My initial suspicion is that dice-related randomness alone makes this impossible.

Examine that assumption critically, including the distinction between solving a deterministic game,
solving a stochastic game, and finding optimal strategies under uncertainty.

The article should be technically substantive but accessible to an interested software-development
audience.

It should build on the previous LinkedIn article rather than simply repeat it.

## P. Final implementation and completeness audit

### P1. Traceability checklist

Before considering the work complete, revisit the original playtesting notes and this consolidated
specification.

Create a traceability checklist containing every individual observation and requested change.

Each item must have one of these statuses:

- Implemented and tested.
- Implemented but not yet fully tested.
- Investigated, with a documented reason for not implementing.
- Awaiting my clarification.
- Not yet implemented.
- Deferred to the roadmap.

Retain the original observation alongside its implementation status so that nothing disappears during
subsequent development sessions.

### P2. Reporting

At the conclusion of each implementation phase, provide a concise report containing:

- What was changed.
- Which bugs were corrected.
- Which features were added or improved.
- Tests performed and their results.
- Any outstanding issues.
- Any decisions requiring my input.
- Any requirements deferred to a later phase.

Do not silently omit requirements simply because they are inconvenient or require substantial
investigation.

### P3. Regression testing

Pay particular attention to regression testing across C2027 and Grand Legion.

A change that fixes one game must not introduce inappropriate behavior in another.

For bot-related changes, retain existing strength measurements and evaluate effects through actual
bot-versus-bot testing wherever possible.

### P4. Persistent project documentation

Ensure that the consolidated requirements, historical decisions, bot-strength measurements, and
implementation statuses are maintained in the repository.

Future development sessions should be able to recover the context without relying on the current
Claude conversation.

The objective is to ensure that the entire history of Zaturnava development and playtesting remains
available, and that every requirement is eventually addressed or explicitly deferred.

## Other defects

1. The "Face Off" option is too understated -- it looks like just a boring text. (It does not help that
   there are a million bright yellow buttons, for passing for rolling, for hiring, for letting green
   take over....) It is something special -- it should be flashy!

   The "Pass" button should be to the left of the "End game" button and more neutral. The "Let Green
   take over" button should be to the left of the "Pass" button and match the color of my ally.

2. Advisors have been seen escaping the palace. I think they escaped captivity and were placed outside
   the palace when they were restored. But the palace was not full. Pieces which are bound to the palace
   should only escape the palace when there is nowhere in the palace left to put them. To be clear:
   Advisors, Guards, and other pieces bound to a palace should be (re)placed as close as possible to
   their initial location within the palace; only if it is not possible -- because the palace is full
   -- should they be placed as close as possible outside the palace (thereby escaping the palace).

3. On almost empty boards with nothing left to do but clean up enemy pieces, pieces should not be
   meandering around randomly (unless they are driven by low level bots).

4. An intelligent bot should not be:
   - Offering or agreeing to ransoms when one team has a massive lead and adding pieces will only
     prolong the game without giving either team a realistic chances of catching up and pulling ahead
     without players are being very unlucky or very stupid.
   - Accepting ransoms where piece they replace will immediately be captured or get either in their own
     way or the way of their teammate.
   - Offering ransoms the player is unlikely to accept unless they are very tired or very stupid.
     Even stupid bots should not do this too much.

5. Intelligent bots should not ignore obvious and safe opportunities to capture enemy leaders and
   powerful enemy pieces -- especially when those pieces are threatening their allies! I think sometimes
   this may happen because they aren't correctly considering the directions pawns/infantry travel and
   don't see it as safe.

6. Intelligent bots should not sacrifice rooks to capture pawns!

7. Check how random values are calculated... There seems to be a bias towards enemy bots rolling
   doubles. Also, doubles seem to be rolled in streaks.

## Notes before consolidating (the same day)

Given before the consolidated text, and covered by it (B7, C2, D1, D2, D3); kept for their wording.

- "King (Tafl) and Swedes should be able to use allies (as well as other pieces under players control)
  for capture. Swedes go berserk when their throne is controlled by an enemy. A berserk Swede will
  capture whoever is in the middle (friend and foe alike) whenever it is maneuvered into position
  making it the third piece orthogonally (either row or column) regardless of which colour piece is on
  the other side. Berserk Swedes can capture both friends and enemies concurrently."
- "Some of bots are getting kind of slow -- even in C2027, especially when there are checker pieces in
  the game. Hypothesis 1: The algorithm to check for multiple jumps is a disaster. Hypothesis 2: Because
  you haven't made game specific bots, GL-bot logic is leaking into how the bots play C2027 and they are
  checking for impossible conditions."

## Said while playing (2026-11-13), on scenarios

- "We agreed all pieces locked into forward motion would be able to do back-to-back, so this is also
  relevant for GL (and if it weren't, it shouldn't have been listed instead of giving errors!)"
- "Are you seriously going to make me match configuration requirements to try out scenarios? ...
  Instead of putting this under 'custom', let's have a third option (e.g. Quick game / Custom /
  Scenarios) where if I select scenarios I get to select from the scenarios and everything else will be
  configured as necessary to test the scenarios."
- The scenario dropdown "is too narrow - I can't even read this."
- "There is no kind of piece 'Rook (Western)'" on starting the Cannons and the Castling scenarios.
