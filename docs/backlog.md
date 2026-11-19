# Backlog

Everything still to be done, in one place, most pressing first. Each item names where it came from: a playtest's
own number (**25**, **N29**), the [2026-11-15 specification](playtesting/2026-11-15-specification.md)'s id (**D2**,
**X4**, tracked row by row in [traceability.md](playtesting/traceability.md)), a stage of the grand game's
[roadmap](grand/roadmap.md), or the date it was asked. What has been done, and the choices made doing it, is in
[done.md](done.md); an item moves there with the change that finishes it. Questions waiting on an answer are in
[open-questions.md](open-questions.md). The order is *Proposed*: the user may reorder it.

## 1. Now: asked for and not yet done

- **Rivers as chosen** (asked 2026-11-19): in a custom Grand Legions game, the creator chooses whether rivers
  bisect or quarter the board, and whether they flow through or around kingdoms; random stays a choice.
- **Shogi mercenaries** (asked 2026-11-19): mercenaries to include every Shogi piece, leaders too.
- **Restriction or weakness** (asked 2026-11-19): a piece's card tells a restriction (may not leave its palace,
  may not cross a river), which the game's rules impose and a lift may remove, from a weakness (bound to squares
  of one colour), which is the piece's own movement and never lifted.
- **A mercenary marked on the board** (asked 2026-11-20): a mercenary promoted into another kind (a Shogi Pawn
  become a Western Rook) stays one by the rules, but the page marks mercenaries only by kind, so it cannot be
  seen. Each piece to say whether it is one (`PieceDto`), and its card and the board to show it.
- **A turn that changes nothing is a pass** (noticed 2026-11-20): a bot moving a Rook c2 to d2 and back, taking
  nothing, leaves the board as it was, a pass with extra steps, yet it starts the count of rounds of everyone
  passing again. In a new edition, a turn ending with the board and the prisoners as they began counts as a pass;
  and the bots never play one when they may pass.
- **Real AI bots** (asked 2026-11-20): bots that learn to play both games, Grand Legions above all, seeing where
  the pieces stand and how they move, not "backgammon in 2D" as Athena does now (her network ignores positions).
  With no paid tokens: a network trained here on the board square by square, per
  seat's view, by self-play, and run on the server.

- **D2** At least three non-random bots below 1200 Elo, a real progression of difficulty; Icarus sits only a few
  points below Midas. Keep every past measurement. *Begun* (2026-11-19): Narcissus and Epimetheus judge each move
  as Sage does, looking no further than the move, and play any of their better half or best third at random.
  Still to do: once the bot-rating job has rated them, check they fall below 1200 and between Random and Midas,
  and adjust their shares if not.
- **M1–M5** Offline tournaments and training. *Begun* (2026-11-19): `scripts/wsl-overnight.sh` trains Athena and
  rates every bot, cycle after cycle, until stopped, each round kept as it ends
  ([local-development](contribute/local-development.md#smart-app-control-windows)). Still to do: resuming a
  tournament mid-way, not only from the last whole round (M2); the results into the repository, and through it
  the live site's ratings, by a second documented command (M4); every bot rated at every ruleset, with past
  measurements kept beside new ones (M5).

## 2. Bots that play well

From the specification's observations of the bots in play (F, X) and the Sage plan:

- **X5** Safe captures of leaders and strong pieces, above all those threatening an ally, ignored; perhaps from
  misjudging which way infantry move, and so what is safe.
- **X6, F1** Rooks sacrificed for Pawns; a Raja left blocking an allied Pawn, which could neither promote nor
  escape attackers.
- **F2, F3, F4** Promotion chosen badly: into a Raja of another army rather than the Rook that wins, losing
  control of an ally's pieces. Bots should weigh the promoted piece's owner, mobility and effect on coordination,
  head for the throne that matters, see an enemy's chance to seize control, use Rooks, push Pawns, and turn a
  winning position into a win.
- **X3** Meandering on a near-empty board with nothing left but to clean up, except for the weakest bots.
- **X4** Ransoms offered or accepted when hopeless or harmful: prolonging a decided game, bringing back a piece
  that is at once taken or in the way, offering what no one sensible would accept.
- **F6** Resigning a hopeless endgame: a strategic judgement, not a points comparison, each bot by its own
  threshold.
- Sage, next: command and empires (thrones and leaders weigh them only in part), and the enemy's reply. With the
  reply weighed, Sage may take its own team's pieces (rules §6.4) when that saves a better one (*Stated*
  2026-10-29).
- Sage's newer measures (the pieces' promotion, reach and retreat; leaders attacked, thrones in danger, armies
  commanded; `under-escapes`) start at no weight: tune them, and teach Athena them.
- *(Stated 2026-11-05)* Trusting the ally by how well it plays. *(Proposed)*: the ally's rating as a feature, so
  the ally-side weights can grow with a strong ally and shrink with a weak one.
- *(Stated 2026-11-05, 2026-11-06)* Shared command: handing the turn to a regent ally when it would play the rest
  better; giving up the leader on purpose so that an ally commands the army; passing up a ransom or escape that
  would bring the leader home, so the ally keeps command. *(Proposed)*: learned rather than written in.
- Athena, then: the kind of ending as a third prediction, so that agreeing to end becomes a move it can judge; the
  board square by square, turned to each seat's own view, once there are hundreds of thousands of games; people's
  games mixed in as they accumulate (*Proposed*).

## 3. Grand Legions: bots and armies

- **D4** Strong Grand Legions bots by family, each preferring its game's pieces while playing well: Western,
  Xiangqi, Janggi, Shogi, Makruk, Checkers, Tafl, and Fairy (pieces none of the others use). Where a family lacks a
  piece, decide whether to add it or use the nearest.
- **E4** Several army-selection strategies (many cheap pieces, a few dear ones, a balance across the classes),
  shared or not; clever bots answering what the others pick and place. Stage 4: Random at random, Midas by value,
  Solomon evenly. Later, bots of their own leanings: one origin, sliders, infantry; an army of different bots.
- *(Stated 2026-11-12)* Fairy-Stockfish as a bot. *Begun*: it plays
  ([self-play](bots/self-play.md#fairy-stockfish-as-a-bot)); its rating is still to come. In the BotRunner only,
  in an adapter project of its own so its GPL-3 licence stays with it.
- *(Stated 2026-11-12)* Evaluate an OpenSpiel adapter: any number of players and chance, and MCTS,
  AlphaZero-style self-play and CFR to make bots with; weigh porting the rules against a bridge to ours.

## 4. Grand Legions: rules still to come

- Stage 5: upgrades and promotions bought in play, in a palace with the player's own leader on its throne,
  lowering the score; lifts bought in the draft, and kept in the game; the four-throne bonus, once for each set
  of leaders; bots that spend.
- Stage 3: several leaders in one army, with everything the rules say of "the Raja" said of leaders, and throne
  control by any of them; scoring by piece value, the Chaturaji 2027 bonuses times 10, until playtesting says
  otherwise (**Q14**). Check what of this the composition edition already does.
- Stage 6: start rows as a setting; the face-off's optional pause when it first opens.
- Stage 2: choosing a quick setup for one's own kingdom in a game set up by hand.
- Stage 2: saved settings, named, shared and copied to edit ([specification §2.1](grand/specification.md#21-saved-settings)).
- *(Stated 2026-11-12)* **Kingdoms placed by the players**, as a setting of the game's creator, before their
  palace, throne and pieces; probably still keeping to the board's corners, more or less, so that no later player
  is left without room.
- **3** Variant pieces: two Chinese-chess-like elephants, one confined by two rivers dividing the board in four,
  a dearer one that may cross them; the boat to be replaced.
- Boat triumph (§9): a nice-to-have; the condition is so unlikely that no one may miss it.
- Stage 7: playtest, and act on what it asks: the Chaturaji 2027 bonuses, the leader bonus of 60, the Pawns'
  promotion premiums ([pieces](grand/pieces.md#still-open)) (**Q14**).

## 5. Look, feel and players

- Grand Legions' lifts (**N25**, Stage 7): a badge above a piece's symbol for each kind of restriction lifted,
  once lifts are kept in the game.
- A player's card (*Stated* 2026-11-12): beyond their standing at the game, which it shows, their other games.
- **29** One person, one identity: a person may play several seats of one team when the game allows it, without
  pretending to be several people.
- *(Stated 2026-11-12, low priority)* The table's panels (the draft's list of pieces, the turn's panel, the log
  and talk) resized and placed by each player, as a piece card already may be dragged.
- **2** Pieces drawn as pictures of their own; for now chess glyphs, and the boat a drawn sailing boat.
- **N29** (low) An encyclopedia of pieces: each piece's names, the games it comes from and that use it here, its
  variants, and how it moves, captures and what else it may do.

## 6. For developers and outside AIs

- Bring your own AI, next: a push channel, so a program need not poll; the engine and the bot interface as
  packages, so a bot need not reference the repository's projects (*Proposed*: Athena's network among them, so
  the sample bots, in repositories of their own, may play by it); replaying a history without .NET.

## 7. Research and writing

- **O1–O5** An article on AI in Zaturnava, following the first on LinkedIn: what was tried, why, and what worked;
  other and hybrid approaches; what differs between an AI for Chaturaji 2027 and one for Grand Legions; chess's
  tactics, backgammon's dice and Go's negotiated endings as paradigms; whether the game could ever be solved.

## 8. Later

- *(Stated 2026-11-19, low priority, to consider)* Whether a mercenary's liabilities, or a berserk piece's, should
  lower its price.
- *(Stated 2026-11-19, very low priority, an idea)* **Combat by dice**, as in Loka: a capture is not certain, but
  rolled for, the odds weighed by such things as who attacks and whether the square has defenders. Optional, and
  only in games with dice (not with **Dice off**); what weighs, and how, still to be worked out.
- **N1–N5** Secondary palaces: not to be built before promotions and lifts can be bought
  ([roadmap](grand/roadmap.md#later)).
- More kingdoms than four, or of unequal size; more pieces; games without palaces.
- *(Stated 2026-11-12, not a priority)* Fold into Grand Legions the pieces Fairy-Stockfish's variants have that it
  lacks, each into `docs/Other Potential Pieces.xlsx` and the game, with its own symbol.
- A mechanical bot that likes to roll, rather than always moving without dice.
- *(Stated 2026-11-05)* Private team chat, and how well bots plan through it; moot while our bots do not talk.
- *(Stated 2026-11-12)* Upgrade to .NET 11, once MonsterASP hosts it; the SDK is pinned (`scripts/pin-sdk.py`,
  and each workflow's setup-dotnet version).
- *(Stated 2026-10-31, an idea)* **Games made by others, as data alone**, described in JSON or XML and played on
  the live server, combining only what the engine has, no code. *(Proposed)*: a versioned, checked schema; the
  engine's building blocks named and parameterised; limits on size and on a turn's work; uploads tied to a
  signed-in player, listed apart from the house's; the dumb bots able to play any described game.
- *(Stated 2026-11-19)* **Playable scenarios**, beside the test scenarios there are now (development server only):
  the game's creator sets up part or all of the board, and it is played as a real game, people joining or bots
  given any seat. No faked dice in them. They do not count toward a player's Elo, or the scenario has an Elo of its
  own, so that no one can farm Elo with lopsided setups. Overlaps **Games made by others** above: a playable
  scenario is a starting position made by a player rather than a whole game. *(Proposed)*: the scenario format
  shared by both kinds, a playable one marked so; made in the page (pieces placed by hand) as well as uploaded;
  listed apart from ordinary games. In the games lists they are filtered on their own, apart from test
  scenarios (Stated 2026-11-19).
