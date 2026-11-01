# Backlog

What the first playtest (2026-10-24) asked for, sorted by kind. Numbers are the playtest's own, so
that each item can be traced to the note it came from. Items move to "Done" with the change that
did them.

## Seeing what happens

- **25** In the lobby, say when each game started (who plays, how many moves and which seats are
  open are shown).

## Look and feel

- **2** Pieces drawn as pictures of their own; for now they are chess glyphs, and the boat a
  drawn sailing boat.
- **3** A choice between classic chaturanga pieces and "modern" ones (the elephant is effectively
  a rook).

## Rules not yet played

- **21, 22, 26** (rest) The idle-rounds safety net as a per-game setting.
- Boat triumph (§9): a nice-to-have; the condition is so unlikely that no one may miss it.
- Ransom (§8): bots that offer during their turn (they offer as they pass, or once it is over).

## Players and identity

- **29** One person, one identity: a person may play several seats of one team when the game
  allows it, without pretending to be several people.
- **34** Games survive a server restart (storage that persists, and identities to go with it).
- **35** Player statistics: games played and won, points scored...
- The page's preferences (theme, sound, pieces, the log's highlights) kept with the player's identity
  once there is one, not only in the browser.

## Bots

- *(Stated 2026-10-26)* A bot that learns from past games and analyzes the current one.
- *(Stated 2026-10-29)* Players can play against an AI, and the claim must mean something: a bot that
  has learned the game, measured against the hand-tuned ones, not a language model picking among
  moves it does not understand. The scores and how a game ended (agreement, one team left, a team
  giving up...) matter to what it learns, not only who won.
- *(Stated 2026-10-29)* Pythia (Sage asking a free AI service which of its three best moves to make)
  was removed: it added lag and spent quota without a reason to think it played better. It is kept
  in the repository's history, at the tag `archive/pythia`.
- In order (*Stated 2026-10-29*, the user agreeing to the plan):
  1. **Recorded games**: every game kept with its seats (which bot, or a person), its actions, the
     scores and how it ended, downloadable as data; self-play writing the same records.
     - Done: a game's history (`GET api/games/{id}/history`, the MCP tool `game_history`) and every
       finished game's (`GET api/history`), in the shape the BotRunner now writes self-play games in;
       each outcome names its kind of ending as a code (`agreement`, `one-team-left`...).
     - Games are kept only as long as the server's storage keeps them: in memory unless it is given a
       database ([local-development.md](contribute/local-development.md#running-the-game)).
  2. **Sage** (under way): it looks ahead through the dice (expectimax), scoring positions by
     weighted features, the weights tuned by BotRunner self-play.
     - Done: looking through the rest of its own turn, the dice averaged, and weighing points,
       pieces, leaders, prisoners, thrones, and what each side could take next. With its starting
       weights it beat Argus 75-22-3 in 100 self-play games (seats swapped halfway). Its weights are
       tuned by self-play (BotRunner `--train`) and kept as data for each ruleset: two rounds took
       score down to 0.7, material up to 1.3, prisoners down to 0.35 and exposure up to 1.3, and Sage
       so tuned beat Sage as it was 124-84 in 208 games.
     - Done: mobility (how many more cells its team attacks than its enemies do), and the ruleset's
       own measures (`IPositionMeasuring`); for Chaturaji 2027, how far its Pawns have come. Two more
       rounds of tuning (60 games a side for each nudge) kept material up to 1.69, mobility at 0.3 and
       chances up to 0.39; the Pawns' progress won no weight.
     - Next: command and empires (thrones and leaders weigh them only in part), and the enemy's
       reply. The Chaturaji 2027 features serve the grand game too (grand/specification.md §6.1).
     - *(Stated 2026-10-29)* With the enemy's reply weighed, Sage may take its own team's pieces
       (rules §6.4), when that saves a better one; until then no bot does.
  3. **A learned bot**, the AI to play against: a small neural network, trained by self-play and on
     recorded games, judging positions inside Sage's look-ahead through the dice. Free, offline, no
     keys.
     - *(Proposed)* First as TD-Gammon learned backgammon: the network trained by temporal
       differences from self-play. As KataGo does for Go, it predicts more than the winner: each
       team's final score, and how the game will end, so that agreeing to end becomes a move it can
       judge by how agreed games turned out. AlphaZero's additions (a network suggesting moves, and
       a tree search guided by it) only if that stops improving; different bots trained different
       ways if both prove worth playing.
     - *(Proposed)* Mostly self-play at first, as AlphaZero learned; people's games mixed in as they
       accumulate, as AlphaGo began, since only they show how people agree to end a game.
     - Measured before it is offered: against Sage in BotRunner self-play, seats swapped halfway.
     - Done, *(Proposed: the name, and each choice below)*: **Athena**, Sage's look-ahead through the
       dice with each position judged by a network: one hidden layer of 32 units, and direct weights
       from each input, predicting who will win and by how much. It agrees to end a game when ending
       it now is worth as much as playing on, as the network judges playing on. BotRunner `--learn`
       trains it on game histories: every position replayed, seen from every seat, taught how the game
       went for that seat's team (the final result, not yet temporal differences). It learned from
       2,000 games: 1,600 of Sage against itself, and against Argus and Midas; and 400 of Athena's own
       against Sage, so that it saw what its mistakes led to. Against Sage it won 41, lost 58 and drew
       1 of 100 games, then 46 to 54 in the ratings' 100: nearly as strong, not stronger. Rated 2626,
       beside Sage's 2689.
     - Athena's play showed that a bot passing could offer a ransom the rules refuse (a prisoner that
       may not go home yet, or a gift to a retired ally), which stopped the game; now no bot does.
     - Learned on the way: a network weighing each square, from so few games, judged moves by chance
       and lost all 100 to Sage, so the board is given summed up, each army's pieces of each kind
       counted, and how far they have come, beside Sage's own counts. Learning only from the positions
       a turn begins with failed too, as Athena judges the positions in the middle of its turn.
     - Under way *(the plan agreed 2026-10-30)*: the network starts out judging as Sage does (its
       direct weights Sage's own), so that it learns only corrections to Sage; it learns by temporal
       differences (λ 0.8); and it learns in rounds, each playing 400 more games against Sage, seats
       swapped halfway, to learn from in the next (`scripts/athena-rounds.py`). It is called stronger
       than Sage only at 55% or more of 400 games.
     - Then: the kind of ending as a third prediction, and the board square by square, turned to each
       seat's own view, once there are hundreds of thousands of games.
  4. **Bring your own AI** (*Stated 2026-10-29*): players connecting their own AI, to play and to
     train it: through MCP (already possible, `Zaturnava.Mcp`) and the HTTP API; recorded games to
     download; and a self-play kit (BotRunner, with a bot of one's own). Documented for developers,
     and reachable from the game itself.
     - Done: [docs/bots/](bots/README.md), for developers: MCP, the HTTP API, game histories, and
       self-play, the BotRunner seating bots from an assembly of one's own (`--bot-assembly`, also
       taken by `rate-bots.py`). "How to play" and "The bots" link to it, on GitHub.
     - Done: [a sample bot](../samples/SampleBot/README.md), standing alone, mechanical, playing the live server
       through the HTTP API, with notes on putting an AI in it.
     - Next: a push channel, so that a program need not poll; the engine and the bot interface as
       packages, so that a bot need not reference the repository's projects (*Proposed*: Athena's value
       network among them, so that the sample bot, moved to a repository of its own, may play by it);
       replaying a history without .NET.
- *(Stated 2026-10-29)* Once the game is online, free AI bots are offered for players to play
  against, subject to whatever policies their hosts require.
- **A bot using Claude**: shelved (*Stated 2026-11-01*). No one will pay for its tokens: not the owner, and
  players are unlikely to give their own key. Without a trained instance to call, it would be an expensive, slow
  random player. The plumbing for a bot paid for each use (`BotProfile.IsPaid`) stays, unused. An AI of a
  player's own joins instead: in a custom game, a seat may be left for "Your own AI", which takes it by MCP or
  the HTTP API with an AI token, and the table shows the game's id while seats are open.

## Talking

- **27, 28** Chat, to everyone or to chosen players (a teammate).

## Later

- A mechanical bot that likes to roll, rather than always moving without dice (not a priority).
- Bots should weigh where a ransomed piece lands, not only what it is worth: it may come back between
  a rook and the piece it guarded or attacked.
- **3** Variant pieces: two Chinese-chess-like elephants, one confined by two "rivers" that divide
  the board in four, and a dearer one that may cross them; the boat is to be replaced.
- The grand game, of which Chaturaji 2027 is a part: its [specification](grand/specification.md),
  [pieces](grand/pieces.md) and [roadmap](grand/roadmap.md).
- *(Stated 2026-10-31, low priority, an idea)* **Games made by others, as data alone.** Anyone may describe a
  game of their own (its board, pieces, how each moves, the dice, the setup, how it is scored and ends) in JSON
  or XML, and play it on the live server, the one the itch.io page plays on. No one may send code: every
  structure, movement and rule a description names must already exist in the engine, so a description can only
  combine what is there. *(Proposed)* What it would take, roughly:
  - a published schema for a description, versioned, checked on upload, with a clear refusal naming what is
    unknown or out of bounds;
  - the engine's building blocks (topologies, movement patterns, dice faces, promotions, captures, scoring and
    endings) named and parameterised, so a description refers to them by name, the way Grand Legions' pieces
    already combine rider, leaper and hopper parts ([pieces](grand/pieces.md));
  - limits on size (cells, pieces, seats) and on the work a turn's legal moves may take, so that no description
    can slow the server for everyone else;
  - an upload tied to a signed-in player, and a listing of others' games apart from the house ones, which the
    house may take down;
  - the dumb bots (Random, Midas) able to play any described game, as they play any ruleset through its legal
    actions.

## Done

- When the enemy on a player's throne passes on the move it makes for them, the player may still make it
  (rules.md §6.3): a rule the engine had lost.
- Accepting a ransom scores a point for each prisoner it sends home (rules.md §8).
- A player left with only their Raja may retire from the game at the start of their turn; their turns are
  skipped from then on, and taking their Raja scores no one (rules.md §10).
- A "How to play" guide opens from the settings bar, in the lobby or at the table: the aim, the pieces,
  turns and dice, points, thrones and command, prisoners and how the game ends.
- "The bots" opens beside it: each bot, easiest first, with its Elo rating at the ruleset, from games
  between every pair of them (`scripts/rate-bots.py`, Random at 1000). The lobby's quick game is easy
  (the three easiest bots), medium (any three) or hard (the three hardest).
- A development server offers scenarios, games set up part-way, to start from (`scenarios/`).
- **7** Not a rules bug: only the first turn of the game uses one die. It looked that way because
  no one's roll was shown (5).
- **24** A game of bots now plays on: the table asks the server for one bot decision at a time
  (`advance`), at `Table:BotPaceSeconds`, and the server no longer plays bots inside a request
  (`Games:MaximumBotSteps` is 0).
- **23** The page remembers your seat for each game in the browser, and the lobby offers "Back to
  seat N".
- **9, 11** Pieces that can move have a ring, the picked-up piece a stronger one, and the cells it
  can reach a dot (a ring where it would capture); the last move is shaded.
- **5, 8** The table keeps a log of everyone's rolls, moves and captures, and each die says which
  piece it allows.
- **14** Bots move one after another at a readable pace, with sounds that can be turned off.
- **4, 16, 17** On your turn a panel says "Your move", with the dice and large Roll and Pass
  buttons.
- **18** The thrones are marked in their army's colour.
- **25** (partly) The lobby shows who plays each game, how many moves it has had, and its open
  seats.
- **1** A dark theme by default, and a light one.
- **2** (partly) Pieces are drawn as chess glyphs: raja ♚, elephant ♜, horse ♞, boat ♝, pawn ♟.
- **31, 32, 33** The board starts from your own corner and turns 90° either way, and back.

Second playtest (2026-10-25):

- **30** Every seat rolls off for who goes first, ties rolling again, and the winner plays that
  die as the opening move; the log shows each roll.
- A move made without rolling says so in the log ("..., without rolling"), so it is clear why no
  roll is shown; every move names its cells ("from b5 to c5"), and the board names every cell.
- **19** (partly) A Raja on another seat's throne lets its seat move that army too, as does an
  ally with no Raja left (rules §6.1); an enemy Raja on a seat's throne forces it to roll, and
  then moves for it (§6.3).
- Throne bonuses: +1 for a Raja entering an enemy's throne, +2 more for taking a Raja on its own
  throne.
- **21, 22, 26** (partly) The game ends when only one team has pieces (that team wins), when only
  Rajas are left (a draw), or when no one has rolled or moved for three rounds (points decide); a
  seat with no pieces is skipped.
- **15** Each seat shows the prisoners it holds.
- **2** (more) The boat is a drawn sailing boat, not a bishop.
- The page and server start together from Visual Studio's **Play** launch profile, and the README
  says how to play.

After the second playtest (2026-10-25):

- A commanded enemy army may take any piece (rules §6.4), by default; taking a piece of one's own
  team scores nothing.
- Promotion (§8): a Pawn on the far edge may become any captured piece, which its original owner
  then commands; at once (the page opens its choices) or later (the page rings the Pawn, and picking
  it up opens them). An enemy promotes only with a die that moves a Pawn.
- Dice no piece can use are told as lost, in the log and on the page, before the turn passes.
- Bots: Midas promotes into its team's best piece and trades prisoners at a profit; Solomon, a new
  bot, gives allies' prisoners back and trades evenly. No offer may be made twice in a turn.
- The lobby gives each bot one seat at most, and starts a quick game at a random seat with the bots
  shuffled.
- The pieces show as traditional (Raja, Elephant, Horse as silhouettes) or chess (King, Rook,
  Knight), as the player chooses.
- A page whose request fails keeps looking, rather than stopping.
- **15** Ransom (§8): on their turn, and in a timed window once it is over, a player may offer
  prisoners back, for prisoners in return or as a gift; the receiver accepts or declines against
  the clock (bots answer at once).

Toward the grand game's [Stage 0](grand/roadmap.md#stage-0--finish-what-the-grand-game-inherits):

- Argus, a new bot, looks at the reply before it moves: it weighs each action by what it gains, less
  the worst an enemy could do in return (take a piece left unguarded, win an exchange, or step a
  leader onto its empty throne). It trades prisoners as Solomon does. In 40 games of self-play, a
  team of two Arguses beat a team of two Midases 38 times.

After playing against Argus (2026-10-25):

- A commanded enemy army may no longer take its commander's own team's pieces, by default (rules
  §6.4): a player took their own Pawn so, and could never ransom it back from themselves.
- The log says when a capture was made with another army's piece, and who holds the prisoner:
  "Yellow took Black's Elephant, with Red's Elephant, and holds it prisoner (+4)."
- Bots mind the table when they trade with an enemy: they never give a leader back, and make no trade
  that leaves their team weaker against that enemy's, counting what each has on the board. So an
  even trade suits only the team that is behind, and nothing given back revives a team that has
  nothing left.
- Ransom (§8): when a piece going home has several free squares as near its own, its owner chooses
  among them, marked on the board (a Pawn need no longer land in the Boat's corner).
- Once a game is over, the table offers to play it again (the same seats, played the same way, the
  player at the same seat) or a quick game.
- A promotion's choices list the player's own pieces first, then their allies', then the rest, each
  group the most valuable first.
- Escape on doubles (rules §7): a prisoner of the kind the doubles show, of an army the player
  commands, goes home instead of the double move, taking an enemy that stands there.
- The empire bonus (rules §5): taking the last enemy Raja scores each player of that team who
  commands an ally +1, and the captor +2 more, or +4 if the Raja was on its own throne.
- **19** (rest) Voluntary regency (rules §6.2): a seat whose ally's Raja holds its throne, or that
  has no Raja, may let that ally take over the rest of its turn, with its own army only.
- **20, 21, 22, 26** (more) Ending the game by agreement (rules §10): a red End game button, apart
  from the turn's other buttons, proposes it, waiting for the others or passing on all one's turns
  meanwhile. Each other player is asked in turn, against the clock, no answer counting as no; if all
  agree, points decide, and if one refuses, the game goes on where it was. A player who gave up their
  turns may rejoin the game with a green Rejoin game button, which takes the End game button's place.
- A fanfare when an enemy Raja is taken, and a lament when one of the player's own team's is. The log's
  highlights tell when a player gains or loses control of another's army, complete (playing that army's
  turn as regent) or partial (moving it on their own turns, because their Raja holds its throne, or an ally
  has no Raja), and when a team's empire begins or ends.
- When a player leaves a game unattended, the other players may vote, after two minutes, to stop waiting
  for them, and their turn passes; three such turns in a row and they have given up their turns. The
  absent player is told at once, with a flashing Rejoin game button that gives them their two minutes
  back.
- A die that nothing can use is greyed out, until a move frees a piece it lets move. The log keeps
  every highlight for as long as the game lasts, not only those among the latest 40 events; showing
  everything, it says where older events were let go.
- Passing no longer opens a window to offer ransoms, which the player could have offered before passing;
  the window is left for a turn that ended on its own. Bots offer as they pass, instead.
- Argus no longer counts a lone piece on its throne as keeping an enemy Raja off it.
- Sounds tell a Raja back on the board (hopeful or ominous) and command or an empire begun or ended (good
  or bad for the player's team) apart from a Raja taken; when both happen, one plays after the other.
- A bot paid for each use (tokens) is seated only when chosen in the lobby: never by default nor in a
  quick game. The lobby says it costs tokens, and playing again with it is a red button that says so.
- Two allied Pawns back to back may swap places, as a Pawn's move (rules.md §3); the board marks the
  swap with a dashed ring, and the log tells it.
