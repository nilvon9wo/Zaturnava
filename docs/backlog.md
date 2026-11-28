# Backlog

Only what is left to do, one row per item, most pressing first within each part. The detail lives where the link
points; what is already done, and the history of items done in part, is in [done.md](done.md); questions waiting on
the designer are in [open-questions.md](open-questions.md). An item moves out of here with the change that finishes
it. The order is *Proposed*: the user may reorder it.

| Status | Meaning |
|---|---|
| To do | Nothing built yet |
| Begun | Some of it built; the row says only what is left |
| Check | Built; waiting to be seen working (live, or over the ratings) |
| Waiting | Waiting on the designer |
| Idea | Stated as an idea or low priority; not planned yet |

Sources: a playtest's number (**25**, **N29**), the [2026-11-15 specification](playtesting/2026-11-15-specification.md)'s
id (**D2**, **X4**; [traceability](playtesting/traceability.md)), a stage of the
[Grand Legions roadmap](grand/roadmap.md), or the date asked.

## 1. Bots (first of all)

The user, 2026-11-27: "Intelligent bots and especially high quality AI bots are MUCH more important than the
lifts... the difference between having a playable game or not." The plans:
[ai-assessment.md](bots/ai-assessment.md) (learned bots), [strategy-plan.md](bots/strategy-plan.md) (mechanical).

| Item | Status | What is left | Source |
|---|---|---|---|
| Athena for Grand Legions | To do | No network yet for any Grand Legions size: train one, board-seeing, from the tournament games and her own | 2026-11-27 |
| Athena, further | To do | The kind of ending as a third prediction; people's games mixed in as they accumulate; search with the network (row 4 of the assessment: MCTS, policy + value) | ai-assessment |
| Sage presses home | Check | See over the ratings whether Sage now finishes off a beaten army (game 3b5d749e, turn 22); if not, weigh a capture made ready | 2026-11-20 |
| Safe captures taken | To do | Leaders and strong pieces left untaken, above all those threatening an ally | **X5** |
| No pointless sacrifices | To do | Rooks given for Pawns; a Raja blocking an allied Pawn | **X6, F1** |
| Promotion by command | Begun | The choice by who will command the piece, beside safety and worth | **F2–F4** |
| Promotion races | To do | Take the enemy Pawn about to promote, and the one blocking an ally's Pawn from promoting, before a dearer piece far from anything (the user, 2026-11-28: Green's Rook took a lone Horse instead); and an enemy Pawn about to promote onto a square the team attacks is let promote and taken, not blocked (the same day: Green blocked it with the user's Boat, which then could not move, and no points came of it); the promotion measure weighs nothing yet | 2026-11-28 |
| No meandering | To do | Clean up a near-empty board rather than wander | **X3** |
| Trades that prolong | Check | A decided game prolonged by ransoms, if one is seen | **X4** |
| Resigning by judgement | Begun | Beyond points and hope: hope against the points in play; a second reason (a leader, a heavy, a Pawn about to promote) per temper | **F6**, strategy-plan |
| Command and empires | To do | Sage weighs command and empires fully, and the enemy's reply; then taking its own piece to save a better one (rules §6.4) | 2026-10-29 |
| Sage's newer measures | To do | Promotion, reach, retreat, leaders attacked, thrones in danger, armies commanded start at no weight: tune them, teach Athena them | strategy-plan |
| Trusting the ally | Idea | The ally's rating as a feature, ally weights growing with a strong ally | 2026-11-05 |
| Shared command | Idea | Handing the turn to a better regent; giving up a leader so an ally commands; learned rather than written | 2026-11-05 |
| Tactician, deeper | To do | Ten times its search budget, measured against itself | 2026-11-27 |
| Grand Legions endgames | To do | Hand-set endgame starts for Grand Legions, as Chaturaji 2027 has | 2026-11-27 |
| Bot ratings | Begun | Every bot rated at every ruleset; the large board's fit (*Proposed*: rate from pairs both sides sometimes win; bots by bot games alone) | **D2**, **M1–M5** |
| Fairy-Stockfish's rating | To do | It plays; rate it | 2026-11-12 |
| Army strategies | To do | Bots drafting their own way (many cheap, few dear, balanced; one origin, sliders); answering what others pick and place | **E4**, Stage 4 |
| Bots that spend | To do | Buying upgrades, promotions and lifts in play, once those exist | Stage 5 |
| OpenSpiel | Idea | Evaluate an adapter: many players, chance, MCTS, AlphaZero, CFR | 2026-11-12 |
| A bot that likes to roll | Idea | A mechanical bot that rolls rather than always moving without dice | — |

## 2. Grand Legions rules

Detail in the [roadmap](grand/roadmap.md).

| Item | Status | What is left | Source |
|---|---|---|---|
| Buying lifts, made plain | To do | Buy a lift with its piece, in the draft, seeing what it does and costs; on the card, "With lifts" beside "Pieces in the way", the drawing alone changing on click | 2026-11-27 |
| Purchases in play | To do | Upgrades, promotions and lifts bought in a palace with one's own leader on its throne, lowering the score | Stage 5 |
| Four-throne bonus | To do | Once for each set of leaders | Stage 5 |
| Recruiting in play | To do | Fully specified ([roadmap](grand/roadmap.md#stage-5--points-to-spend)); bots must recruit, guard and spoil | 2026-11-27 |
| Player out when leaderless | Waiting | Designer thinking it over: whether its prisoners may escape back, and whether a leader brought back brings the player back | 2026-11-25 |
| Saved settings | To do | Named, shared and copied to edit ([spec §2.1](grand/specification.md#21-saved-settings)) | Stage 2 |
| Kingdoms placed by the players | To do | A creator's setting, before palace, throne and pieces, kept near the corners | 2026-11-12 |
| Playtest | To do | Then act on it: the Chaturaji 2027 bonuses, the leader bonus of 60, the Pawns' promotion premiums | **Q14**, Stage 7 |
| Dealing's spread measured | To do | Measure how often each piece is dealt: the outliers, dearest and cheapest of a class, under 10% (better under 5%), the rest weighted to the middle; tune `QuickArmy.Spread` to it, keeping the peak at 80% of the median | 2026-11-28 (P4) |
| What taking several is worth | To do | Count chains of captures (Checkers, Tafl) in real games, and in games of a bot that seeks them; price them as an expert would use them, not a layman | 2026-11-28 (P9) |
| End proposals time out | To do | A proposal to end the game times out for players who do not answer, the answer defaulting to keeping the game going | 2026-11-28 (P15) |
| A lift kept on promotion | To do | A promoted piece keeps a lift where it still means something for its new kind (lets it do what it could not otherwise) | 2026-11-28 (P17) |
| Forcing a won game | To do | What rules §10 calls giving up one's turns is the leading team *forcing* its victory, when the other side has no reasonable comeback and could only drag the game out, boring the others and throwing off the ratings (the user, 2026-11-28: "give up" is confusing, it sounds like conceding). Rename it so. Judge it by more than a points lead: points, material on the board, turns without any score changing, and turns where everyone or almost everyone just passes (the idle, quiet and repeat rounds of the safety nets already end some such games). The margins relative to the game: 10 may suit Chaturaji 2027, not alone; Grand Legions' is 100 (× 10), to check against its piece values | 2026-11-28 (P14) |

## 3. Look, feel and players

| Item | Status | What is left | Source |
|---|---|---|---|
| Janggi Soldier's card | Check | Palace diagonals should now show; check in the next live game dealt one | 2026-11-25 |
| Lift badges | To do | A badge above a piece's symbol for each restriction lifted, in play | **N25** |
| Rating systems, further | Begun | The design's extensions (dice and army luck, dice offset, deviation over time away); comparing how well each predicts; grouping strengths by the chosen system | [design](ratings-design.md) |
| Pieces under their own game's names | Idea | Family bots' armies named by their game ([roadmap](grand/roadmap.md#stage-7--looks)) | 2026-11-26 |
| One person, one identity | To do | One person playing several seats of a team, as themselves | **29** |
| Panels placed by each player | Idea | Draft list, turn panel, log and talk resized and moved | 2026-11-12 |
| Pieces as pictures | Idea | Drawn pieces, beyond the chess glyphs | **2** |
| Encyclopedia of pieces | Idea | Each piece's names, games, variants and moves | **N29** |

## 4. Developers, outside AIs and writing

| Item | Status | What is left | Source |
|---|---|---|---|
| Bring your own AI, next | To do | A push channel; the engine and bot interface as packages (Athena's network among them); replaying a history without .NET | — |
| Article on AI in Zaturnava | To do | What was tried and worked; hybrids; Chaturaji 2027 against Grand Legions; whether the game could be solved | **O1–O5** |
| .NET 11 | Waiting | Once MonsterASP hosts it | 2026-11-12 |

## 5. Later

| Item | Status | What is left | Source |
|---|---|---|---|
| **Zaturnine Zphere**, a third game | Idea | Hexes over an icosahedral sphere, persistent, many players, shifting alliances, fog of war ([roadmap](grand/roadmap.md#later)) | 2026-11-25 |
| Playable scenarios | Idea | A creator's starting position played as a real game; no faked dice; no Elo farming | 2026-11-19 |
| Games made by others, as data | Idea | Games described in JSON or XML from the engine's building blocks | 2026-10-31 |
| Custom pieces | Idea | Pieces described in a file, priced by the sheet's rules | 2026-11-25 |
| Custom pricing | Idea | Prices set by the creator, or moving with how pieces are bought and fare | 2026-11-25 |
| Secondary palaces | Idea | Not before purchases in play ([roadmap](grand/roadmap.md#later)) | **N1–N5** |
| More kingdoms, pieces; no palaces | Idea | More than four or unequal kingdoms; games with bare thrones | Roadmap |
| Fairy-Stockfish's pieces | Idea | Fold its variants' missing pieces into the catalogue | 2026-11-12 |
| Combat by dice | Idea | Captures rolled for, as in Loka; only with dice on | 2026-11-19 |
| Mercenary and berserk prices | Idea | Whether their liabilities should lower their price | 2026-11-19 |
| Private team chat | Idea | And bots planning through it; moot while bots do not talk | 2026-11-05 |
