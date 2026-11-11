# Backlog

What the first playtest (2026-10-24) asked for, sorted by kind. Numbers are the playtest's own, so
that each item can be traced to the note it came from. Items move to "Done" with the change that
did them. Questions waiting on an answer are gathered in [open-questions.md](open-questions.md).

## Going public (playtest of 2026-11-04)

Numbered **N1**, **N2**... after that playtest's own notes. In the order planned (*Proposed*), most pressing
first.

- **N2** The database stays as it is through every change, by migrations; never wiped (*Stated*).
- **N3** A leaderboard, for players to compete for; with each player's Elo and their **experience**: each time
  a player is on the winning team, they gain their own points plus their team's (*Stated*). Each player's
  statistics also count the games they won, lost, drew and abandoned (*Stated*). *Done*: a **Leaderboard**
  button in the page's bar lists, game by game (opening at the table's game, else the game last started:
  *Stated*), the top 100 signed-in players by Elo, with their experience and
  games won, lost, drawn and abandoned. A game is abandoned by a player while it is not over, has no guest at it
  (such games are purged instead), has been untouched for 24 hours, and waits for that player's own move; waiting
  for someone else's, it counts for no one, and once the player moves it counts no more (*Stated* 2026-11-06).
  Each finished game counts once, within a minute of ending, for every
  signed-in seat but one their own AI played; everyone starts at 1500, K is 32, and each team is rated as the mean
  of its seats (a bot's by its rating at the ruleset, a guest's 1500), its expected score the average against
  each other team (*Proposed*).
- The lobby's list of games:
  - **N20, N21** Filter by players or bots (wildcards allowed, any seat), several at once ("Bob and Joe, and a
    seat open"). *Done*: a Players box takes names separated by commas, "Bob, Jo*, open"; `*` for any letters,
    `open` for an open seat, `person` for anyone's, a bot by name or id, case not counting (*Proposed*).
  - **N20** Each game says when it was created, whether it has started, when its last move was made, and
    whether it waits for someone to take an open seat or for a player who went away to come back. *Done*: the
    lobby says when each game was created and last changed, whether it waits for players or has not started, and
    who at it is away ("Yellow away"), as the game was last saved (`awaySeats`); a game not saved since then shows
    no one away until its next move.
  - **N20** Abandoned games are purged: any game with a guest in it and no activity for 24 hours, and any game
    waiting for a signed-in player who has not signed in for 31 days. *Done*, for games not over only
    (*Proposed*: a finished game's history is public and rates its players). Waiting for a player means it is
    their seat to decide, and the game unchanged for 31 days too; a player not signed in since when they last
    did was kept is never counted gone (*Proposed*).
  - **N20** On signing in, a player is reminded of every game waiting for their move, oldest first, and
    encouraged to end those they no longer play and resume the oldest of the rest. *Done*: the lobby of a
    signed-in player opens with **Waiting for your move**, every game whose seat to decide they claimed (not
    one their AI plays), the one changed earliest first, each with a **Back to** button that resumes the seat
    held in this browser or else takes it back (`GET api/players/me/awaiting`). Signing in or out reloads the
    lobby.
  - **N20** A signed-in player may take back their own seat from any device, signed in as themselves
    (*Stated* 2026-11-06), so that the reminder can list every game waiting for them, wherever they claimed
    it. *Proposed*: the server hands that browser a new token for the seat, and the old one no longer works.
    *Done*: in the lobby, a game whose seat the player signed in claimed, and this browser does not hold, has a
    **Back to** button, which takes the seat back (`POST api/games/{id}/seats/{seat}/take-back`). A seat their
    AI plays stays the AI's (*Proposed*).
- **N24** Athena takes Pawns with its Raja and Rook (Elephant) when that only loses them: not a real sacrifice.
  *Done*: Athena now subtracts a twentieth of what an enemy could take next from its network's judgement
  (*Proposed* weight). Against Sage it went from about 63 Elo behind to about 17 behind.
- **N31** In a game all but won, Athena played as if at random: it passed over a sure taking for a roll, drifted
  pieces about, moved a guarded Elephant somewhere worse, and parked a Boat on an enemy throne, where only an allied
  Raja would do any good. Its network judged every move there all but alike, and its guess at the margin swung by
  six points between quiet moves. *Done* (*Proposed* weights): it no longer counts the network's margin for a game
  going on; Sage's appraisal, at a five-hundredth of a unit (`NetworkValuing.Steadiness`), settles what the network
  leaves even; and each of its team's pieces other than a Raja on an enemy throne costs it two of those units
  (`NetworkValuing.Blocking`): less than a Pawn taken, and far less than a piece kept from being taken, so that it
  still goes onto a throne to take or to escape (*Stated*: boats should attack an enemy, a Raja most of all, on a
  throne), but neither parks nor lingers there. Against Random, Argus and Sage it now rates about 47 Elo above Sage.
- **N32** The lobby lists the games waiting for a signed-in player's move, but not their other games in progress:
  a game waiting on someone else is found again only through the Players filter. A **Mine** list (*Stated*).
  *Done*: the lobby's **Show** offers a player signed in **Mine**: their games not over, at seats they claimed signed
  in, whoever's move it is (`GET api/games?player=`).
- AI clients (*Stated*): two complete sample clients that play on Zaturnava, one by **MCP** and one by **UTCP**.
  Both decide by the same shared project Athena uses, compile and run apart from Zaturnava, and document how a
  developer makes their own bot from them. MCP: *Done* as [McpSampleBot](../samples/McpSampleBot/README.md), for
  now with the HTTP sample's mechanical mind, until the engine and Athena's network are packages it can take
  (below). UTCP: *Done*: the server serves a [UTCP manual](bots/utcp.md) at `utcp` (the MCP tools, by the same
  names, each a call to its HTTP route), and [UtcpSampleBot](../samples/UtcpSampleBot/README.md) plays by it, with
  a small UTCP client of its own, as there is no C# one yet.
- Grand Legions (preview until these are done):
  - **N25** Letters as pieces, ambiguous ones, make it nearly unplayable: it needs its own pieces' symbols.
    *Done*, as asked: every piece has a symbol of its own, as chess's pieces have theirs (King, Queen, Rook,
    Bishop, Knight), and the elephants the elephant; like pieces have like symbols (Stated; see
    [spec §8](grand/specification.md#8-the-page)). A class's emblem with letters, as first done, was not
    playable alone; the class's emblem is now drawn small beneath the symbol, so that the class is told too, with
    every piece's own abbreviation beside it (Stated). The emblems are pictures, not a map's frames, which were too
    alike and abstract to tell apart (Stated 2026-11-06): a flag, crossed swords, a stirrup, a weight, a bomb; the
    Courtier a plumed cap (Proposed). Rivers wave, rather than run straight. Still to come: lifts as small badges above the symbol, one
    per kind of restriction, once a piece's lifted restrictions are kept in the game.
  - Each game's pieces priced on its own board, from its actual dimensions, not values set per size (*Stated*
    2026-11-06; [pieces](grand/pieces.md#prices-for-each-game)): on the small board the Giraffe cannot move
    from the centre, yet costs what it does on 8 × 8. *Done*: reach counted from the centre of the actual
    board, and the promotion premium over the actual distance, adjust the sheet's value.
- The piece cards (*Stated* 2026-11-06): a **Cards** setting at the table turns them off and on, remembered;
  and a card may be dragged by its title anywhere on the screen, where it opens from then on. *Done*. The lobby
  says, under Quick game and Custom, that Custom is how to play other people.
- **N29** (low) An encyclopedia of pieces: each piece's name here, its other names, the games it comes from
  (Chess, Xiangqi...), the games here that use it, its variants (kinds of Pawn, of Elephant...), and how it
  moves, captures and what else it may do.

## Seeing what happens

- **25** In the lobby, say when each game started (who plays, how many moves and which seats are
  open are shown). *Done*: each game says when it was created and last changed ("created 3 h ago, last change
  5 min ago").

## Look and feel

- **2** Pieces drawn as pictures of their own; for now they are chess glyphs, and the boat a
  drawn sailing boat.
- **3** A choice between classic chaturanga pieces and "modern" ones (the elephant is effectively
  a rook).

## Rules not yet played

- **21, 22, 26** (rest) The idle-rounds safety net as a per-game setting.
- Boat triumph (§9): a nice-to-have; the condition is so unlikely that no one may miss it.
- Ransom (§8): bots that offer during their turn (they offer as they pass, or once it is over). Worth doing
  (*Stated 2026-11-07*): offered at the start or in the middle of a turn, a trade may bring back a piece of one's
  own to where it can take something at once, or send an enemy's home to where it can be taken at once; and a
  trade made before moving gives less away of how the player means to use what comes back.

## Players and identity

- **29** One person, one identity: a person may play several seats of one team when the game
  allows it, without pretending to be several people.
- **34** Games survive a server restart (storage that persists, and identities to go with it). *Done*: the
  live server keeps games and players in its database (N2).
- **35** Player statistics: games played and won, points scored... *Done* in part, with the leaderboard (N3):
  games won, lost, drawn and abandoned, and experience; and points scored, the player's own in every game they
  finished, won or not (*Proposed*), counted for games finished from 2026-11-07 on.
- Players' Elo ratings (*Stated 2026-11-01*). *Done*, with the leaderboard (N3), as N3 says, and as follows
  (*Stated 2026-11-07*): a game counts toward Elo only while its players stay the same: it may count at
  first, but stops counting the moment any seat changes hands, for any reason (a bot taking over from someone
  away, a person from a bot), since a seat's rating could otherwise be higher, lower or unknown at different
  times; and no seat a guest's (*Stated 2026-11-01*). Nor does a game count toward Elo in which one player has
  several seats, their own and their AIs' (a player against their own bots, or their bots against each other),
  however many: one side could play badly to raise the other. Such a game still counts in the rest of the
  player's standing: won, lost or drawn, experience and points (*Proposed*).
- A player may seat several AIs of their own in one game (*Stated 2026-11-07*), each saying which it is, so
  that a player's bots are told apart. *Done* (*Proposed*: how): an AI claiming a seat for a player keeps the
  nickname it gives, shown as "Sage (Ada's AI)"; without one, "Ada (AI)". Still for the user to try by hand: an
  outside bot playing by the API or MCP.
- Leaderboards for bots (*Stated 2026-11-07*): one of the bots alone, and ones showing how people fare against
  the bots. *Done* (*Proposed*: how): the leaderboard offers three boards. **Players** is as before. **Bots** lists
  the server's bots, most games won first, each seat a bot played counting, with their record against people alone.
  **People against bots** lists the players by their games against bots alone: every seat not on their team a
  bot's. Counted from 2026-11-07, not for games before.
- Every leaderboard sorts by any of its columns, its header clicked (Elo, experience, won, lost, drawn,
  abandoned), and shows ten at a time, paging for more (*Stated 2026-11-07*). *Done* (*Proposed*: how): a header
  clicked sorts most first, clicked again fewest first; the server sends up to 1000 of each board, and the page
  sorts and pages them itself (a larger population would want the server to page instead). The Points column is
  no longer shown, as it said little that experience does not (*Proposed*); the API still gives `points`.
- The names on the leaderboards open the player's, or the bot's, profile (*Stated 2026-11-07*). *Done*: as a
  name at a table does.
- A player's profile filled out (*Stated 2026-11-07*): a description of their own, and a choice of which of the
  services they sign in with others may see.
- A winning streak on the leaderboard, perhaps: open question [OQ9](open-questions.md#oq9-a-winning-streak).
- A player's username unique (*Stated 2026-11-07*): no player may take a name another has. The profile's field
  is called **Username**.
- A signed-in player setting up a game may bar guests from it (*Stated 2026-11-01*). *Done* (*Proposed*: how):
  a custom game set up signed in may be **Players signed in only**; its open seats then refuse a guest, and the
  lobby asks a guest to sign in to join it. Playing such a game again keeps it for players signed in only.
- A person may take a seat over from a bot, where the game allows it (*Stated 2026-11-01*, low priority).
  *Done* (*Proposed*: when): anyone watching a game not over, at which they hold no seat, has a **Take over**
  button on each bot's seat; the seat is theirs for the rest of the game
  (`POST api/games/{id}/seats/{seat}/take-over`). No player signed in may play two seats so; which seats a guest
  holds, only their browser knows, so it offers the button only to a guest watching. Whether a guest may is the
  game's own setting, as for its open seats (*Stated 2026-11-07*): a guest may, unless whoever set the game up
  chose players signed in only. Such a game no longer counts toward Elo.
- A custom game's board size chosen freely, within limits, rather than only small, medium or large
  (*Stated 2026-11-01*); likely to come with the game setup rework. One of the two most noticeable gaps in
  custom games (*Stated 2026-11-07*).
- A custom game's pieces chosen by hand and placed into each kingdom, rather than only dealt (*Stated
  2026-11-07*): the other of the two most noticeable gaps.
- The page's preferences (theme, sound, pieces, the log's highlights) kept with the player's identity
  once there is one, not only in the browser. *Done*: a player signed in keeps the theme, sound, safety,
  pieces, piece cards, log, the talk's order and AI talk, previews and how the lobby sets games up with the
  server as they choose them (`PUT api/players/me/preferences`), and every browser they sign in on takes them
  up; seats held, a game's own setup and where the piece card was dragged stay with the browser (*Proposed*).

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
     - Done *(Stated 2026-11-05; the split Proposed)*: every weight but exposure and chances is told
       apart by side: the bot's own seat's, its ally's and its enemies'. A single number in the
       weights' JSON still weighs all three alike, so weights tuned before keep playing as they did;
       tuning nudges each side on its own.
     - *(Stated 2026-11-05)* Trusting the ally: how far to rely on the ally's pieces should depend on
       how well the ally plays. *(Proposed)*: the ally's rating as a feature, so the ally-side weights
       can grow with a strong ally and shrink with a weak one; learned by Athena, or tuned in Sage
       against allies of several strengths.
     - *(Stated 2026-11-05)* Pieces decomposed: a piece's worth comes from its reach, its
       restrictions, how fast it moves, whether it can go back, and how near it is to promoting. Those
       parts, measured where the piece stands (little reach at the edge, a Pawn a step from promoting),
       may say more than the piece's fixed worth. *(Proposed)*: count them as measures of their own.
       Done *(Proposed: each measure)*: beside the Pawns' progress, Chaturaji 2027 and the grand game now
       measure how near each army's Pawns are to promoting (one over the rows each has still to go), its
       pieces' reach where they stand (their moves, all told), and how many of those moves go back. They
       start at no weight, for tuning to find theirs; Athena's network, trained before them, plays on
       seeing only the measures it knew. Next: tune them, and teach Athena them.
     - *(Stated 2026-11-05)* Safety and shared command: the safety of every side's leaders, not only
       the bot's own; the safety of thrones, as losing a throne loses command of an army; and command
       shared through thrones and regency, so that moves can be coordinated and one piece may move on
       more turns of a round than its own army's (taking, then getting away before the counterattack).
       Done *(Proposed: each measure)*: Chaturaji 2027 and the grand game measure, for each seat, how
       many of its leaders an enemy could take next (`leaders-attacked`); how many of the thrones it
       commands by are in danger (`thrones-in-danger`: its own, should an enemy leader be able to step
       onto it or take its leader there, and each other throne its leader sits on, should an enemy be
       able to take that leader); and how many pieces of other armies it commands on its own turns
       (`commanded`). As every measure is, each is weighed apart for the bot's own seat, its allies and
       its enemies, starting at no weight for tuning to find. Next: tune them; and a bot that hands its
       turn to a regent ally (no bot does yet) when the ally would play the rest of it better.
     - *(Stated 2026-11-06)* Along the same line: giving up the leader, so that an ally commands the
       army; and passing up a ransom or an escape that would bring the leader home, so that the ally
       keeps command of it. Probably worth more than handing a turn over, unless the ally is the
       stronger player (higher Elo). *(Proposed)*: learned rather than written in. Sage already weighs a
       ransom and an escape by the whole position once it is done, so `commanded`, weighed for the
       ally, counts what bringing a leader home takes from the ally; tuning finds how much. Giving the
       leader up on purpose needs Sage to look past the enemy's taking it, which it does not yet do; the
       ally's rating is for the bot to weigh as trusting the ally above says. Midas, Solomon and Argus
       weigh a ransom by worth alone, and keep doing so.
     - *(Stated 2026-11-06)* Pieces in an enemy's kingdom, and above all on its throne, stand where
       escaping prisoners land (rules §7), so entering and sitting there is a risk. Done *(Proposed)*:
       a fourth measure, `under-escapes`, what a seat's pieces are worth that stand on the starting
       square of an enemy's piece now held prisoner, by anyone; weighed apart by side, starting at no
       weight. Only prisoners held now count; a square some later prisoner might land on does not.
     - *(Stated 2026-11-05)* Private team chat, and how well bots plan through it, is another thing
       to measure; moot while our bots do not talk, and not with bots that spend tokens.
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
     - Done: [a sample bot over the HTTP API](../samples/HttpApiSampleBot/README.md), standing alone, mechanical, playing the live server
       through the HTTP API, with notes on putting an AI in it.
     - Done: [a sample bot over MCP](../samples/McpSampleBot/README.md), its twin: the same loop and
       mind, through the server's MCP tools, by the protocol's C# SDK.
     - Done: [a UTCP manual](bots/utcp.md) at `utcp`, and [a sample bot over UTCP](../samples/UtcpSampleBot/README.md)
       playing by it.
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

- **N26** Checked: at every size of Grand Legions, small included, each leader starts on its own throne (in the
  small game, where the kingdoms touch, that is the board's corner); a test now keeps it so.
- **N27, N28** The choice of chaturaji or chess pieces is offered only at a table whose pieces it changes (a Raja,
  Elephant or Horse in play): not in the lobby, nor at Grand Legions, whose pieces, Elephants among them, are
  always shown by their own names, never "translated" as Rooks.
- **N23** A bot's name at the table opens a profile of the bot, as a person's does: what it does, whether it asks
  an AI (and costs tokens each use), and its rating at the game.
- **N12** Escaping names each prisoner once ("Escape Black's Boat, held by Yellow"). Choosing it marks the
  squares it may go to (a ring where it would take a piece), and a click on one frees it there; clicking
  anything else leaves it a prisoner.
- **N4, N5** Games not ready for the public (Grand Legions now) are *preview* games: named so ("Grand Legions
  (preview)"), and neither offered nor listed in the lobby unless the player ticks "Show preview games", which the
  browser remembers. A game says so itself, in its rules: no feature flags.
- **N22** A profile no longer has a free-form links field: the profile shows the player's name, and the column is
  dropped from the database by a migration.
- **N13–N18** The lobby's New game panel: Quick game or Custom above the game; every choice's dropdown in one
  column; Play at the bottom right; each game set up as the player last started it, and the game last started
  chosen; a game played at several sizes set up at its middle one (*Proposed*: Grand Legions, medium).
- **N19** The lobby's bar stays at the foot of the window.
- **N1** The bar tells which build the page is ("Build 1a2b3c4", its commit), so an old copy shows itself.
- **N10** The log tells an escape as "Black's Horse escaped from Red, to b1.", or "Green helped Black's Horse
  escape from Red, to b1." when an ally's doubles freed it.
- **N9** A player who passes on all their turns is still offered ransoms; accepting one rejoins the game
  (rules.md §10).
- The theme button shows the theme in use ("Theme: dark"), as the other settings buttons show theirs.
- A retired player's ally, while it has a Raja, commands any piece the retired player gets back (a Pawn
  promoted into it); the retired player may rejoin to play it, and only retired Rajas end the game
  (rules.md §10).
- **N6** Once a team's only pieces left are retired Rajas, the game ends: a draw if that team has as many
  points as the other or more, else the other team wins (rules.md §10).
- **N7, N11** A seat with no pieces on the board still takes its turns while any of its pieces is held
  prisoner, or it holds prisoners: it may roll to let a piece escape, and offer ransoms (rules.md §10).
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
  Rajas are left (a draw), or when no one has moved for three rounds (points decide); a
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
