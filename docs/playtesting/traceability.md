# Playtesting traceability

Every observation and request from the
[2026-11-15 specification](2026-11-15-specification.md), with what has been done about it. Each row keeps
its item's id in that file, so the observation itself is always one click away. Rows are never deleted:
a status changes, and the note says how and in which pull request.

Statuses (the specification's P1): **Done** (implemented and tested), **Done, part-tested**,
**Not doing** (investigated, reason given), **Asking** (awaiting the user), **To do**, **Roadmap**.

## B. Game mechanics and piece behaviour

| Id | Item | Status | Notes |
|---|---|---|---|
| B1 | Promotion at the right-hand edge, a game option, off by default | Done | Lobby checkbox for Grand Legions, remembered as last set; the far edge's promotion into a captured piece, at the edge to the piece's right as its army faces (Proposed: a piece's own promotion still needs the far edge). Scenario field `promotesAtSide`. |
| B2 | Back-to-back: Black d2 cannot swap with Green d3 | Done | Only Pawns could swap in Grand Legions. Now any two allied pieces that move only forward swap: Pawns of every kind, the Lance, the Berolina Pawn and the Man (`Swappers`). Guide text says so. Tests: `GrandLegionsRulesTest` back-to-back theories; scenario `forward-only-swaps`. |
| B3 | Janggi General captured scores nothing, is never ransomed | Investigated | A plain capture by an enemy scores 68 and takes the General prisoner (test `Apply_WhenAThreeleaperTakesAnEnemyJanggiGeneral_ScoresItAndHoldsItPrisoner`). The rules give 0 and no ransom only when an ally takes it (§6.4: given back) or its seat has retired. The game id is asked in open-questions.md. |
| B4 | Janggi Elephant beside a river cannot move right | Investigated | Rivers do not hold it back: a test leaps it over a river to its right. It is blocked only by a piece on its leg (one step, then the first diagonal). The game id is asked in open-questions.md. |
| B5 | Xiangqi Elephant across the river on a 26 × 26 board, 23 × 3 kingdoms | Done | From edition 8 the Elephant may not cross the river between where it stands and where it would go, nor enter a cell the river cuts; one standing across from its kingdom's corner moves on that side. Tests: `RiverCrossingTest`, `GrandBoardTest.Crosses_*`. |
| B6 | Guard card shows general diagonals | To do | With C2. |
| B7 | Tafl King and Swede: custody with allies and pieces controlled; berserk when their throne is enemy-held | Done | From edition 8 an ally's piece holds as well as any piece the mover commands. While an enemy holds the custodian's throne it is berserk and takes any piece, friend or foe, held between it and any other piece, several at once. Guide text updated. Tests: `JumpAndCustodyTest` (ordinary, with allies, berserk on and off, friend and foe at once). |

## C. Piece cards

| Id | Item | Status | Notes |
|---|---|---|---|
| C1 | Legends show only what applies (War Engine: no move, no forward) | To do | |
| C2 | Card toggles: move or capture; palace or river; blocked, with pieces, not red Xs; Tafl custody's second piece | To do | |
| C3 | Animated illustrations | To do | Not at the cost of accuracy. |
| C4 | No horizontal scroll for cards | To do | |

## D. Bots

| Id | Item | Status | Notes |
|---|---|---|---|
| D1 | A bot not knowing a game is not offered for it | Done | Each bot says which games it knows (`IBotPlaying.Knows`). The catalog sends each bot's rulesets (`BotDto.Rulesets`); the lobby's strengths and the server's seat filling skip a bot that doesn't know the game. Reviewed: Athena, with no network for a game, played just as Sage, so it is offered only for Chaturaji 2027. Fairy-Stockfish only where it can hold the board. Pythia keeps its own search with Sage's judgement, and the rest use generic, rules-driven logic, so they know every game. Tests: `AthenaBotTest`, `FairyStockfishBotTest`, `GameServiceTest`, `BotCatalogTest`. |
| D2 | Three or more non-random bots below 1200 | To do | Keep every past measurement. |
| D3 | 30-second cap, best move so far; slow with Checkers and Swedes; both hypotheses investigated | Done | Every search stops at 30 seconds (`Deadline`) and plays its best so far. Tactician's look counts as spent; Pythia stops passing and plays the most-tried action; Sage values unexplored positions as they stand. Tests: `DeadlineTest`. Found: every `Apply` worked out all legal actions again to check the move, so a search paid for move generation twice at every step. Bots now use `ApplyLegal`, which skips that check. Hypothesis 1: since edition 6 each jump is a move of its own, linear to generate; only games dealt before edition 6 enumerate every chain, which can grow exponentially on a crowded board. Hypothesis 2: no Grand Legions logic runs in Chaturaji 2027. Custody, palaces and rivers are configured per variant and return at once when absent. |
| D4 | Grand Legions bots by family: Western, Xiangqi, Janggi, Shogi, Makruk, Checkers, Tafl, Fairy | To do | |

## E. Army selection

| Id | Item | Status | Notes |
|---|---|---|---|
| E1 | Default mix: a leader per 20, half infantry, the rest even | To do | |
| E2 | Budget assumes 80% of kingdom and palace filled; no palace-bound preference | To do | |
| E3 | Cost inversely related to frequency | To do | |
| E4 | Several strategies; clever bots respond to others' picks | To do | |
| E5 | Budget settable for random armies too | To do | |

## F. Promotion and strategy

| Id | Item | Status | Notes |
|---|---|---|---|
| F1 | Green Raja blocking a Black Pawn; Rook sacrificed to a Pawn | To do | |
| F2 | Promoting into a Black Raja, not a Green Rook | To do | |
| F3 | Promoting into a Raja, not the Rook that wins; meandering; not heading for the throne that matters | To do | |
| F4 | Promotion, thrones, control, endgames, Rooks, Pawns, urgency | To do | |
| F6 | Resigning hopeless endgames | To do | |

## G to M

| Id | Item | Status | Notes |
|---|---|---|---|
| G | Evangelist, as the spreadsheet defines it | To do | |
| H1 | Lobby layout: games right, waiting games above, New game hidden behind a button when there are games to join | To do | |
| H2 | Shareable link to a game with open seats | To do | |
| H3 | Kingdom sizes checked before a game is made | To do | |
| H4 | Games list as a sortable table, one row each, who is next and how long | To do | |
| I | Placement previews, desaturated and blinking | To do | |
| J1 | Escape and hire as a list, as ransom is | To do | |
| J2 | Clicking a listed piece shows its card | To do | |
| J3 | Promotion choices inspectable | To do | |
| K | Loading progress on itch.io | To do | |
| L | Remove "The bots" button | To do | |
| M1–M5 | Offline tournaments, resumable, ratings for every bot, results into the repository | To do | |
| N1–N5 | Secondary palaces | Roadmap | Not to be built yet (OQ7). |
| O1–O5 | Article on AI in Zaturnava | To do | |

## Other defects

| Id | Item | Status | Notes |
|---|---|---|---|
| X1 | Face-off flashy; Pass neutral, left of End game; Let Green take over left of Pass, in the ally's colour | To do | |
| X2 | Palace-bound pieces put back outside a palace not full | Done | From edition 8, a palace-bound piece going home (ransom, gift or escape), with its own and its kind's squares taken, goes to the free palace squares nearest its own; outside only when the palace is full. Tests: `PalaceTest`, `GrandLegionsRulesTest`. |
| X3 | Meandering on near-empty boards | To do | |
| X4 | Ransoms offered and accepted when hopeless or harmful | To do | |
| X5 | Safe captures of leaders and strong pieces ignored | To do | |
| X6 | Rooks sacrificed for Pawns | To do | |
| X7 | Dice: doubles favouring enemy bots, in streaks | Investigated | Every seat's dice, people's and bots', come from one `RandomDice` over `Random.Shared`, each face equally likely; doubles are by face (3 + 4 is not doubles), so 1 roll in 6. A 60,000-roll test finds doubles 1 in 6, and 1 in 6 just after doubles too (no streaks). The roll-off for first turn rerolls ties, which can look like a run of doubles. No change. |

## Said while playing (2026-11-13)

| Id | Item | Status | Notes |
|---|---|---|---|
| S1 | Back-to-back scenario for Grand Legions | Done | `forward-only-swaps` (Makruk with Western Pawn, Lance with Shogi Pawn); `pawns` too. |
| S2 | Scenarios a lobby mode of their own, set up as they need | Done | **Scenarios** beside Quick game and Custom: every scenario of the game, any size, started at its own ruleset, the player at the seat to move, medium bots elsewhere. Gone from Custom. |
| S3 | Scenario dropdown too narrow | Done | Spans the panel. |
| S4 | "There is no kind of piece 'Rook (Western)'" | Done | The local game was running a build from before #276, which named only pieces already dealt; restarted on the current code. The square-board inputs seen then were the same old build. |
| S5 | Remember every setting as last set | Done | Armies, budget, secret drafting, players only, side promotion and board size, per game, besides the seats. |
