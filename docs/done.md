# Done

What the [backlog](backlog.md) asked for and has been done, kept for the reasons and choices behind it: what was
*Stated* (by the user) and what was *Proposed* (by the program). Numbers are the playtests' own, so that each item
can be traced to the note it came from. The playtest of 2026-11-15 is tracked apart, in
[playtesting/traceability.md](playtesting/traceability.md).

## Going public (playtest of 2026-11-04)

- **N33** The sign-in corner, top right, tidied (*Stated* 2026-11-11): no more "Signed in as ... Profile, Sign out,
  Delete account" laid out in a row. The corner shows **Sign in, or choose a name**; a guest's alias, with
  "(guest)"; or the player's username with a ▾ opening **Profile and sign-in services**, **Sign out** and **Delete
  account**. The server holds each guest's alias for twelve hours from when the page last asked after it
  (*Stated* 2026-11-12), offering "Ada 2" when "Ada" is taken
  ([http-api.md](bots/http-api.md#a-guests-name-for-the-visit)). A guest claiming a seat sends their alias token
  too, and a guest's name that a player goes by, or another guest holds, is refused (*Proposed*): no guest can sit
  under another's alias or a player's username. A name no one holds is anyone's, so a program playing as a guest
  needs no alias (*Stated* 2026-11-12).
- A player's card (*Stated* 2026-11-12): the dialog a name opens, at the table or on the leaderboard, shows how
  they stand at the game, from its leaderboard: Elo, experience, and games won, lost and drawn there; or that they
  are not on it yet; and, since 2026-11-13, their three winning streaks there (of every game, of rated games, of
  games against people), each with the mean Elo it beat, a row each.
- **N2** The database stays as it is through every change, by migrations; never wiped (*Stated*).
- **N3** A leaderboard, for players to compete for, with each player's Elo and their **experience**: their own
  points earned in a win, and as much again times their share of their team's (*Stated* 2026-11-12, as a
  stronger ally gave more), so that carrying a weak ally gains more than being carried by a strong one; alone, it
  is twice their points (`StandingService.ExperienceOf`; past games recounted by the migration
  `RecountExperience`). The **Leaderboard**, a panel of the lobby, lists, game by game, the signed-in players by
  Elo, with their experience, points and games won, lost, drawn and abandoned. A game is abandoned by a player
  while it is not over, has no guest at it (such games are purged instead), has been untouched for 24 hours, and
  waits for that player's own move (*Stated* 2026-11-06). Each finished game counts once, within seconds of
  ending, for every signed-in seat but one their own AI played; everyone starts at 1500, K is 32, and each team
  is rated as the mean of its seats (a bot's by its rating at the ruleset, a guest's 1500), its expected score
  the average against each other team (*Proposed*).
- The lobby's list of games:
  - **N20, N21** Filter by players or bots, several at once. A Players box takes names separated by commas, "Bob,
    Jo*, open"; `*` for any letters, `open` for an open seat, `person` for anyone's, a bot by name or id, case not
    counting (*Proposed*).
  - **N20** Each game says when it was created and last changed, whether it waits for players or has not
    started, and who at it is away ("Yellow away"), as the game was last saved (`awaySeats`).
  - **N20** Abandoned games are purged: any game not over with a guest in it and no activity for 24 hours, and any
    waiting for a signed-in player who has not signed in for 31 days, the game unchanged as long (*Proposed*: a
    finished game's history is public and rates its players, so it is kept).
  - **N20** The lobby of a signed-in player opens with **Waiting for your move**, every game whose seat to decide
    they claimed (not one their AI plays), the one changed earliest first, each with a **Back to** button that
    resumes the seat held in this browser or else takes it back (`GET api/players/me/awaiting`).
  - **N20** A signed-in player takes back their own seat from any device (*Stated* 2026-11-06)
    (`POST api/games/{id}/seats/{seat}/take-back`): the server hands that browser a new token, and the old one no
    longer works (*Proposed*). A seat their AI plays stays the AI's (*Proposed*).
- **N24** Athena took Pawns with its Raja and Rook when that only lost them. Athena now subtracts a twentieth of
  what an enemy could take next from its network's judgement (*Proposed* weight).
- **N31** In a game all but won, Athena played as if at random. It no longer counts the network's margin for a
  game going on; Sage's appraisal, at a five-hundredth of a unit (`NetworkValuing.Steadiness`), settles what the
  network leaves even; and each of its team's pieces other than a Raja on an enemy throne costs it two of those
  units (`NetworkValuing.Blocking`), so that it still goes onto a throne to take or to escape (*Stated*), but
  neither parks nor lingers there.
- **N32** The lobby's **Show** offers a player signed in **Mine**: their games not over, at seats they claimed
  signed in, whoever's move it is (`GET api/games?player=`).
- AI clients (*Stated*): [McpSampleBot](../samples/McpSampleBot/README.md), with the HTTP sample's mechanical mind
  until the engine and Athena's network are packages; a [UTCP manual](bots/utcp.md) at `utcp`, and
  [UtcpSampleBot](../samples/UtcpSampleBot/README.md), with a small UTCP client of its own.
- Grand Legions:
  - **N25** Every piece has a symbol of its own, as chess's pieces have theirs, the elephants the elephant; like
    pieces have like symbols (*Stated*; see [spec §8](grand/specification.md#8-the-page)). The class's emblem is
    drawn small beneath the symbol, with the piece's own abbreviation beside it (*Stated*). The emblems are
    pictures (*Stated* 2026-11-06): a flag, crossed swords, a stirrup, a weight, a bomb; the Courtier a plumed cap
    (*Proposed*). Rivers wave.
  - Each game's pieces priced on its own board (*Stated* 2026-11-06;
    [pieces](grand/pieces.md#prices-for-each-game)): reach counted from the centre of the actual board, and the
    promotion premium over the actual distance, adjust the sheet's value.
- The piece cards (*Stated* 2026-11-06): a **Cards** setting at the table turns them off and on, remembered; a card
  may be dragged by its title anywhere on the screen, where it opens from then on. The lobby says, under Quick game
  and Custom, that Custom is how to play other people.
- **N26** At every size of Grand Legions each leader starts on its own throne; a test keeps it so.
- **N27, N28** The choice of chaturaji or chess pieces is offered only at a table whose pieces it changes; Grand
  Legions' pieces are always shown by their own names.
- **N23** A bot's name at the table opens a profile of the bot: what it does, whether it asks an AI, and its
  rating at the game.
- **N12** Escaping names each prisoner once; choosing it marks the squares it may go to, and a click on one frees
  it there.
- **N4, N5** Games not ready for the public are *preview* games: named so, and neither offered nor listed unless
  the player ticks "Show preview games". A game says so itself, in its rules: no feature flags.
- **N22** A profile no longer has a free-form links field; the column is dropped by a migration.
- **N13–N18** The lobby's New game panel: Quick game or Custom above the game; every choice's dropdown in one
  column; Play at the bottom right; each game set up as the player last started it.
- **N19** The lobby's bar stays at the foot of the window.
- **N1** The bar tells which build the page is.
- **N10** The log tells an escape as "Black's Horse escaped from Red, to b1.".
- **N9** A player who passes on all their turns is still offered ransoms; accepting one rejoins the game.
- **N6** Once a team's only pieces left are retired Rajas, the game ends (rules.md §10).
- **N7, N11** A seat with no pieces on the board still takes its turns while any of its pieces is held prisoner,
  or it holds prisoners.

## Seeing what happens

- **25** The lobby shows who plays each game, how many moves it has had, its open seats, and when it was created
  and last changed.

## Look and feel

- *(Stated 2026-11-12)* A piece card makes plain how each move is made: a square the piece leaps to has a ring, the
  key says so; any other dot farther off, it slides to. The Shogi Pawn takes the piece directly ahead, as in Shogi.
  The Dragon Horse (Shogi) is a Bishop that may also step one square orthogonally, never a Knight's leap.
- **3** A choice between classic chaturanga pieces and chess ones: the pieces show as traditional (Raja,
  Elephant, Horse) or chess (King, Rook, Knight), as the player chooses.
- **1** A dark theme by default, and a light one; the theme button shows the theme in use.
- **9, 11** Pieces that can move have a ring, the picked-up piece a stronger one, the cells it can reach a dot.
- **18** The thrones are marked in their army's colour.
- **31, 32, 33** The board starts from your own corner and turns 90° either way.

## Rules

- Ransom (§8): bots offer during their turn (*Stated* 2026-11-07). Midas, Solomon, Argus, Sage and Tactician offer
  whenever the rules let them, the first trade that suits them, one a turn; each weighs the trade by the position
  once it is done (*Proposed*).
- Bots weigh where a ransomed piece lands: Argus by what an enemy could take once it is done, Sage and Athena by
  the whole position, and every bot refuses one that pens a piece of its team in.
- A retired player's ally commands any piece the retired player gets back; only retired Rajas end the game.
- When the enemy on a player's throne passes on the move it makes for them, the player may still make it.
- Accepting a ransom scores a point for each prisoner it sends home.
- A player left with only their Raja may retire at the start of their turn.
- **7** Only the first turn of the game uses one die.
- **30** Every seat rolls off for who goes first.
- **19** Command through thrones and regency (rules §6.1–6.3), voluntary regency (§6.2).
- Throne bonuses, the empire bonus (rules §5), promotion (§8), escape on doubles (§7), ransom (§8).
- **20, 21, 22, 26** Ending the game: one team left, only Rajas left, three idle rounds, or by agreement (rules
  §10).
- A commanded enemy army may not take its commander's own team's pieces, by default (rules §6.4).
- Two allied Pawns back to back may swap places (rules.md §3).

## Players and identity

- **34** Games survive a server restart: the live server keeps games and players in its database (N2).
- **35** Player statistics, with the leaderboard (N3): games won, lost, drawn and abandoned, experience, and
  points, the player's own in every game they finished (*Proposed*), counted from 2026-11-07.
- Players' Elo ratings (*Stated* 2026-11-01, 2026-11-07): a game counts toward Elo only while its players stay the
  same, no seat a guest's, and no player with several seats, their own and their AIs'. Such a game still counts in
  the rest of the player's standing (*Proposed*).
- A player may seat several AIs of their own in one game (*Stated* 2026-11-07): an AI keeps the nickname it gives,
  shown as "Sage (Ada's AI)"; without one, "Ada (AI)" (*Proposed*).
- Leaderboards for bots (*Stated* 2026-11-07): **Players**, **Bots** (each bot's rating from games between the
  bots, the mean over the game's sizes, with its record against people alone) and **Everyone** (*Stated*
  2026-11-11: players and bots together, each marked 🧑 or 🤖). Counted from 2026-11-07.
- The leaderboard a panel of the lobby (*Stated* 2026-11-11), sorting by any column and paging ten at a time
  (*Stated* 2026-11-07); the server sends up to 1000 of each board, and the page sorts and pages them (*Proposed*).
  Points on every board (*Stated* 2026-11-11). The names open the player's, or the bot's, profile.
- A player's profile (*Stated* 2026-11-07): **About you**, up to 500 characters, and a tick beside each service
  to show it (`PUT api/players/me/profile`).
- Winning streaks on the leaderboard (*Stated* 2026-11-12): **Streak**, **Rated streak** and **People streak**,
  each with the mean Elo of the opposing teams beaten. A draw neither adds to a streak nor ends it; merged players
  keep the longer of each streak (*Proposed*).
- A player's username unique (*Stated* 2026-11-07): a rename to a name another player goes by is refused (`409`); a
  new player whose name is taken gets "Ada 2" (*Proposed*).
- A signed-in player setting up a game may bar guests from it (*Stated* 2026-11-01): **Players signed in only**.
- A person may take a seat over from a bot (*Stated* 2026-11-01): **Take over**
  (`POST api/games/{id}/seats/{seat}/take-over`). Such a game no longer counts toward Elo.
- A custom game's board size chosen freely (*Stated* 2026-11-01, 2026-11-07): Grand Legions' **custom** size,
  board 6 to 26 cells, not necessarily square (*Stated* 2026-11-13), and kingdoms the creator sets, under the one
  ruleset id `grand-legions-custom` (`QuickGame.CustomOf`).
- A custom game's pieces chosen by hand and placed into each kingdom (*Stated* 2026-11-07): the army draft, in turn
  or in secret, with thrones and palaces placed by each player; what is left of it is in the
  [roadmap](grand/roadmap.md)'s Stage 4.
- The page's preferences kept with the player's identity (`PUT api/players/me/preferences`); seats held, a game's
  own setup and where a piece card was dragged stay with the browser (*Proposed*).
- **23** The page remembers your seat for each game.

## Bots

- *(Stated 2026-10-26, 2026-10-29)* A bot that learns, and an AI to play against that means something: **Athena**,
  Sage's look-ahead through the dice with each position judged by a small network, trained by temporal differences
  in rounds against Sage (`scripts/athena-rounds.py`, and `scripts/wsl-overnight.sh` to leave it learning).
- *(Stated 2026-10-29)* Pythia (Sage asking a free AI service) was removed: it added lag and spent quota. It is kept
  at the tag `archive/pythia`. The name now belongs to Monte Carlo tree search judged by Athena's network
  (*Proposed*, [assessment](bots/ai-assessment.md#progress-2026-11-12)).
- **A bot using Claude**: shelved (*Stated* 2026-11-01): no one will pay for its tokens. An AI of a player's own
  joins instead, through a seat left for "Your own AI".
- Weak bots, for beginners (*Stated* 2026-11-12): **Echo** and **Icarus**, which play as Midas one decision in
  four and one in two, and as Random the rest; **Croesus**, Midas but never leaving its team more exposed than
  before (*Stated* 2026-11-13: Midas itself stays as careless as it is).
- Recorded games: a game's history (`GET api/games/{id}/history`, the MCP tool `game_history`) and every finished
  game's (`GET api/history`), in the shape the BotRunner writes self-play games in.
- Sage: expectimax through the dice, weighted features tuned by self-play, weighed apart by side (*Stated*
  2026-11-05); the pieces' promotion, reach and retreat measured (*Stated* 2026-11-05); leaders attacked, thrones in
  danger and armies commanded (*Stated* 2026-11-05); pieces standing where escaping prisoners land,
  `under-escapes` (*Stated* 2026-11-06).
- Bring your own AI (*Stated* 2026-10-29): [docs/bots/](bots/README.md), sample bots over the
  [HTTP API](../samples/HttpApiSampleBot/README.md), [MCP](../samples/McpSampleBot/README.md) and
  [UTCP](../samples/UtcpSampleBot/README.md), and self-play with `--bot-assembly`.
- Free bots offered to play against, once the game was online (*Stated* 2026-10-29).
- Bots budgeting drafted armies (*Stated* 2026-11-12): a bot follows the random fill, but on any square but its
  throne may buy a stronger piece, strength being reach, drawn at random within 75% of the strongest's
  (`FillingArmies`).
- Rating the bots in the background without a command anyone approves (*Stated* 2026-11-12):
  [the rating job](bots/self-play.md#the-servers-rating-job).
- **24** A game of bots plays on, one bot decision at a time (`advance`).
- **14** Bots move one after another at a readable pace, with sounds that can be turned off.
- Argus looks at the reply before it moves; Midas and Solomon promote and trade prisoners.
- A bot paid for each use is seated only when chosen in the lobby.

## Talking

- **27, 28** Chat: the table's talk has a **Table** tab and, for a player, a **Team** tab; what AIs say can be
  hidden.

## Other

- A "How to play" guide opens from the settings bar.
- A development server offers scenarios, games set up part-way, to start from (`scenarios/`).
- **5, 8** The table keeps a log of everyone's rolls, moves and captures; each die says which piece it allows.
- **4, 16, 17** On your turn a panel says "Your move", with the dice and large Roll and Pass buttons.
- **15** Each seat shows the prisoners it holds.
- The page and server start together from Visual Studio's **Play** launch profile.
- When a player leaves a game unattended, the others may vote to stop waiting for them.
- Sounds tell a Raja taken, a Raja back, and command or an empire begun or ended.
