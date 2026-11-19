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
| B3 | Janggi General captured scores nothing, is never ransomed | Investigated | A plain capture by an enemy scores 68 and takes the General prisoner (test `Apply_WhenAThreeleaperTakesAnEnemyJanggiGeneral_ScoresItAndHoldsItPrisoner`). The rules give 0 and no ransom only when an ally takes it (§6.4: given back) or its seat has retired. Not reproduced; the designer has no game id (OQ8, closed) and will say if it happens again: the bug-report button (T4) is for that. |
| B4 | Janggi Elephant beside a river cannot move right | Investigated | Rivers do not hold it back: a test leaps it over a river to its right. It is blocked only by a piece on its leg (one step, then the first diagonal). Not reproduced, no game id (OQ8, closed); to be reported with T4 if seen again. |
| B5 | Xiangqi Elephant across the river on a 26 × 26 board, 23 × 3 kingdoms | Done | From edition 8 the Elephant may not cross the river between where it stands and where it would go, nor enter a cell the river cuts; one standing across from its kingdom's corner moves on that side. Tests: `RiverCrossingTest`, `GrandBoardTest.Crosses_*`. |
| B6 | Guard card shows general diagonals | Done | A piece that steps along palace lines, standing in its palace, is drawn on its card in that palace as it stands, its throne where it really is, turned as its army faces: only the lines through its square show. Elsewhere, or for its kind, the palace may be switched off, to show its own steps on an open board. Tests: `PalaceStepsTest`, `GameViewsTest`, `TableTest`. |
| B7 | Tafl King and Swede: custody with allies and pieces controlled; berserk when their throne is enemy-held | Done | From edition 8 an ally's piece holds as well as any piece the mover commands. While an enemy holds the custodian's throne it is berserk and takes any piece, friend or foe, held between it and any other piece, several at once. Guide text updated. Tests: `JumpAndCustodyTest` (ordinary, with allies, berserk on and off, friend and foe at once). |

## C. Piece cards

| Id | Item | Status | Notes |
|---|---|---|---|
| C1 | Legends show only what applies (War Engine: no move, no forward) | Done | The key lists only the marks drawn: no forward arrow (on the drawing or in the key) for a piece that moves alike forward and back with no river ahead; no plain move mark for a piece that only leaps; the sliding line only where a dot farther off is slid to. Tests: `TableTest`. |
| C2 | Card toggles: move or capture; palace or river; blocked, with pieces, not red Xs; Tafl custody's second piece | Done | Switches under the card's facts: Moves or Captures; Pieces in the way (one or two pieces put where they stop it most, the moves they take faded), in place of the red crosses; By a river or In its palace, off for an open board. A Tafl King's or Swede's captures show one capture by custody: where it moves, the enemy, and its partner beyond. Tests: `GrandLegionsRulesTest`, `Chaturaji2027VariantTest`, `JumpAndCustodyTest`, `TableTest`. |
| C3 | Animated illustrations | Partly done | Pieces put in the way settle in and the moves they block fade out; an enemy taken by custody fades and returns, over and over. Still to do: the piece moving and a capture taking place, step by step. |
| C4 | No horizontal scroll for cards | Done | The key's meanings wrap beside their marks, and the card never scrolls sideways; long words break. |

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
| H1 | Lobby layout: waiting games above the games, New game hidden behind a button when there are games to join | Done | The games waiting for the player sit above the games list rather than beside it, in the same table form; the leaderboard stays on the right. (#285 had moved the games rightmost, which was not asked; undone.) While the page lists a game the player could join, New game is hidden until the **New game** button at the top of the games list shows it; the button hides it again. Tests: `LobbyTest`, `AppTest`. |
| H2 | Shareable link to a game with open seats | To do | |
| H3 | Kingdom sizes checked before a game is made | Done | As the sizes are typed, the lobby says what is wrong: a board side over 26, a kingdom under 3 × 3, a kingdom longer than the board's shorter side (those at the sides lie turned), or neighbouring kingdoms overlapping (width and depth together over the shorter side). Play and the quick-game buttons are disabled until it fits; the server still refuses any board it cannot play. Tests: `BoardFitTest`, `LobbyTest`. |
| H4 | Games list as a sortable table, one row each, who is next and how long | Done | One line a game, one fact a cell: a column for each seat (who holds it, "away", or a Join button), Next (the seat to move), Moves, Created and Last change; Size only when the games listed differ in size. No game id, no ruleset (chosen above the list). Next, Moves, Created and Last change sort it, again the other way; by default the player's own games first, then the longest waiting. Sorting is within the page shown. The seat deciding is now stored with each game (migration `AddGameDeciding`; games under way are replayed once at start-up to fill it). Tests: `LobbyTest`, `GameViewsTest`, `GameServiceTest`, `EfGameStoreTest`, `InMemoryGameStoreTest`. |
| I | Placement previews, desaturated and blinking | To do | |
| J1 | Escape and hire as a list, as ransom is | Done | Pieces that may escape, come back from fleeing, or be placed once hired show as a row of piece icons ("Onto the board"), and prisoners for hire as another ("Hire"). They look like the ransom list, each named in its tooltip; the large yellow buttons are gone for them. The server says which piece each such action brings (`ActionDto.Coming`). Tests: `TableTest`, `GameViewsTest`. |
| J2 | Clicking a listed piece shows its card | Done | With cards on, choosing a piece to escape, to give back or to ask for in a ransom shows its card. A prisoner for hire is now picked first, showing its card, then hired with a button that names the price; picking it again lets it go. Tests: `TableTest`. |
| J3 | Promotion choices inspectable | Done | A small "?" beside each promotion choice, its own or a captured piece, opens that piece's card, even with cards off, without promoting. Tests: `TableTest`. |
| K | Loading progress on itch.io | To do | |
| L | Remove "The bots" button | Done | Gone, with its dialog. A bot's name opens its profile, on the leaderboard and at a table; "How to play" still links to the bots' documentation for developers. The quick-game hint points to the leaderboard. Test: `AppTest`. |
| M1–M5 | Offline tournaments, resumable, ratings for every bot, results into the repository | To do | |
| N1–N5 | Secondary palaces | Roadmap | Not to be built yet. In the [roadmap](../grand/roadmap.md#later), as detailed; OQ7 closed (2026-11-19), the price *Proposed*: the settler and as many points again as it was worth. |
| O1–O5 | Article on AI in Zaturnava | To do | |

## Other defects

| Id | Item | Status | Notes |
|---|---|---|---|
| X1 | Face-off flashy; Pass neutral, left of End game; Let Green take over left of Pass, in the ally's colour | Done | The face-off chance is a large red-to-gold button that pulses and glows; with reduced motion it is outlined instead. Passing and handing the turn over now sit at the foot of the panel, left of End game (or Rejoin): the hand-over first, in the ally's colour, then Pass, plain. Tests: `TableTest`. |
| X2 | Palace-bound pieces put back outside a palace not full | Done | From edition 8, a palace-bound piece going home (ransom, gift or escape), with its own and its kind's squares taken, goes to the free palace squares nearest its own; outside only when the palace is full. Tests: `PalaceTest`, `GrandLegionsRulesTest`. |
| X3 | Meandering on near-empty boards | To do | |
| X4 | Ransoms offered and accepted when hopeless or harmful | To do | |
| X5 | Safe captures of leaders and strong pieces ignored | To do | |
| X6 | Rooks sacrificed for Pawns | To do | |
| X7 | Dice: doubles favouring enemy bots, in streaks | Investigated | Every seat's dice, people's and bots', come from one `RandomDice` over `Random.Shared`, each face equally likely; doubles are by face (3 + 4 is not doubles), so 1 roll in 6. A 60,000-roll test finds doubles 1 in 6, and 1 in 6 just after doubles too (no streaks). The roll-off for first turn rerolls ties, which can look like a run of doubles. No change. |

## Said while playing (2026-11-19)

| Id | Item | Status | Notes |
|---|---|---|---|
| T1 | Exactly one clear call to action: "Done offering", and often Pass | Done | Passing ("Done offering" after a move, among others) is the call to action, large and glowing, whenever it is the way on: nothing to move on the board, nothing to choose, nothing else to do but hand the turn over or offer prisoners. Otherwise it stays plain. Tests: `TableTest`. |
| T2 | End game a desaturated red: plain to see, not a call to action | Done | A muted red, ordinary weight. |
| T3 | Clicking a prisoner in the ransom dialog does not always show its card; it seems to stop near the end of the turn | Done | The card of a piece on the board, looked at (the one just moved, most often), took precedence over the prisoner's. Choosing a prisoner to give, ask for or hire now shows its card in place of it. Tests: `TableTest`. |
| T4 | A "Report a bug" button for players signed in, saving the game and its context as structured data | Done | Under the table's controls, shown only to a player signed in. It opens a dialog to say what went wrong, and the server keeps it as a row in `BugReports`, a column per fact: report id, reporter, game, ruleset, version, actions, who decides, whether over, the reporter's seat if any, what the page showed (its version, the selected cell, the piece whose card was open), the description and the time. The player is given the report's id. Tests: `BugReportServiceTest`, `EfBugReportStoreTest`, `InMemoryBugReportStoreTest`, `SignInApiTest`, `HttpGameClientTest`, `TableTest`. |
| T5 | Cards still a mess | In progress | C1, C2, C4 and B6 done; C3 in part. |
| T6 | Every army at random: a Guard on d2, outside its palace (a1:c3) | Done | The palace pieces were laid out in the palace about the leader's square; then a leader kept to one colour (the Checkers King) was spread to the other colour with the rest, and the game placed its palace about the leader's new square. From edition 9 such a leader stays on its throne. An army filled at random in a draft after its palace was placed keeps that palace too, its throne anywhere in it. Games dealt before replay as they were. Tests: `QuickArmyTest`, `QuickDraftTest`. |
| T7 | A Dragon Horse for hire did not show its card when clicked | Done | Another card already open (a piece going home, say) took precedence. The card last asked for now shows: a piece off the board picked replaces the one open, and a click on the board brings back the board's. With cards off, none shows. Picking a piece whose card was closed shows it again. Tests: `TableTest`. |
| T8 | Points to spend only when drafting | Done | The points to spend are offered for armies dealt at random too, kept on the deal and spent by the dealer in place of the formula. Tests: `QuickArmyTest`, `ScenarioStartTest`, `GameServiceTest`, `LobbyTest`. |
| T9 | Board-size errors make the dialog jump, squeezed into the label column | Done | The error spans the whole row, in room kept for it, so nothing below moves. |

## Said while playing (2026-11-13)

| Id | Item | Status | Notes |
|---|---|---|---|
| S1 | Back-to-back scenario for Grand Legions | Done | `forward-only-swaps` (Makruk with Western Pawn, Lance with Shogi Pawn); `pawns` too. |
| S2 | Scenarios a lobby mode of their own, set up as they need | Done | **Scenarios** beside Quick game and Custom: every scenario of the game, any size, started at its own ruleset, the player at the seat to move, medium bots elsewhere. Gone from Custom. |
| S3 | Scenario dropdown too narrow | Done | Spans the panel. |
| S4 | "There is no kind of piece 'Rook (Western)'" | Done | The local game was running a build from before #276, which named only pieces already dealt; restarted on the current code. The square-board inputs seen then were the same old build. |
| S5 | Remember every setting as last set | Done | Armies, budget, secret drafting, players only, side promotion and board size, per game, besides the seats. |
