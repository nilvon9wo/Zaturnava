# Done

What the [backlog](backlog.md) asked for and has been done, kept word for word as the backlog had it, with every
measurement, decision and reason: what was *Stated* (by the user) and what was *Proposed* (by the program).
Numbers are the playtests' own, so that each item can be traced to the note it came from. Where an item was done
only in part, what is left is in the backlog, and its history here. The playtest of 2026-11-15 is tracked apart, in
[playtesting/traceability.md](playtesting/traceability.md).

## Asked 2026-11-26

- **The lobby, insanely slow** (the user): "A player shouldn't need to see the leaderboard to start a new game."
  *Done*: the lobby's panels load each on their own. *New game*, the games list and the leaderboard show as soon
  as the catalog arrives, without first waiting to learn who is signed in or for the games to be listed. Whether
  *New game* starts open follows the games listed last. A people leaderboard no longer replays every idle game to
  count the abandoned ones: it reads each game's saved summary. That had cost about 2.7 s cold.

- **Start rows, the kingdom's height**: in Grand Legions, the Western Pawn's start rows default to the kingdom's
  depth at every size, unless the game's creator sets others.

- **The setup menu, organized**: *Custom* groups its options under headings, with a space and a rule between them:
  Seats, Armies, Board, Pieces, Play and Players. Under Pieces, the leaders' options are together (all gone, team
  lost; the face-off pause), and so are the infantry's (start rows; promotion at the right edge).

## Asked 2026-11-25

- **The face-off's pause** (*Stated* 2026-11-04, Grand Legions specification §6.4): a setting of the game may
  pause it when the chance first comes, so that the player has it explicitly before anything else happens.

  *Done* (2026-11-26): in *New game*, for a game whose pieces may call a leader face-off, **Face-off ⏸**: seconds,
  from 5 to 120 (*Proposed*), none by default. When a person's General first faces an enemy leader, the table
  shows ⏸ and the seconds left; meanwhile only that player may act, the face-off included, and no bot decides. The
  pause comes once for each chance, however long it then holds; a bot's General decides at once, as before.

- **Start rows as a setting** (*Stated*, Grand Legions specification §3): how many rows count as start rows (2)
  may be a setting of the game.

  *Done*: in *New game*, for a game whose armies have start rows (Grand Legions), **Start rows**, from 1 to 6
  (*Proposed*), the game's own 2 shown until another is typed, and remembered with the rest of the setup. A Western
  Pawn that starts within that many of its army's rows may make its two-square first move; *Play again* keeps the
  number.
- **A player's card, their other games** (*Stated* 2026-11-12): beyond their standing at the game, which it shows,
  their other games.

  *Done*: a player's profile, opened from their name, adds a table **At other games**: for each other game they
  stand at, its name, their Elo, experience, and games won, lost and drawn there, from that game's leaderboard;
  none, if they have played only this one.
- **One rating system for the whole lobby** (asked 2026-11-20, not urgent): the rating system chosen on the
  leaderboard should hold for the whole page, the New game dialog's bot strengths included.

  *Done*: choosing Elo, Glicko-2 or TrueSkill on the leaderboard relabels *New game*'s strengths and bots at once
  ("Hard bot (TrueSkill 16.8–19.9)"), and the choice is remembered for the next visit. The server sends each bot's
  TrueSkill at each ruleset, counted from its Elo as its leaderboard row is; its Glicko-2 is its Elo. As both come
  from its Elo, the strengths group the bots as before.
- **The lobby, faster** (asked 2026-11-25): show the layout at once, each panel (Games, Leaderboard) with its own
  spinner, and *New game* usable before the rest arrives; returning to the lobby should not wait on everything
  again; the ratings cached.

  *Done* (#348): the lobby lays itself out as soon as it knows the games and who is playing. The games list and
  each leaderboard show their own spinner until they come, and *New game* works meanwhile. Returning to the lobby
  shows what it showed last at once and then refreshes it. The server keeps each leaderboard for a minute, or
  until a game is rated, instead of working it out again for every request (*Proposed*).
- **Custom game options**, the first (asked 2026-11-25): a team with no leader left has lost.

  *Done*: **Leaders: All gone, team lost**, among the game's own options in *New game*, off unless ticked, kept
  with the game's first action and remembered for the next game. With it on, once only one team has a leader on
  the board, that team wins at once; if a capture leaves no team any, a draw. A leader off the board does not
  count, however it left, held prisoner or fled as the Tafl King may, until it escapes or comes back (*Stated*
  2026-11-25: "if it isn't on the board, it isn't on the board").
- **Custom game options**, the second (asked 2026-11-25): each player's idle time, set by whoever creates the game,
  the same for anonymous and signed-in players, after which the turn passes.

  *Done*: **Turn time ⏱** among the game's own options in *New game*, in seconds, from 10 to a day; empty, no
  limit, as before. Kept with the game's first action and remembered for the next game. A person (or an AI over
  the API) deciding without a rule's own timer has that long from their last decision, or their turn's start; the
  table counts it down, and once it is up the host passes for them, as when everyone stops waiting for a player
  away, and they have missed a turn. If everyone has already stopped waiting, whichever time is shorter passes the
  turn. The server's bots are never timed. A game no one has open passes the turn once someone opens it
  (*Proposed*).
- **Field promotion** (asked 2026-11-25, Grand Legions only): a piece that has stood on an enemy throne for 10
  turns may, if its player chooses, promote into a Silver General, and the player then also scores the bonus for a
  leader on an enemy throne.

  *Done*: as each army's turn ends, the game counts a turn more for each of its pieces, leaders apart, standing on
  the throne of a seat not its ally; a piece that leaves, or is taken, starts again from nothing. Once a piece has
  10, its player may promote it into a Silver General, free, and scores the throne-entry bonus, as a leader
  stepping onto that throne does. Only the army's own turns count, ten rounds (*Stated* 2026-11-25: "the piece
  needs to sit there for awhile to earn the promotion").

## Going public (playtest of 2026-11-04)

- **N33** The sign-in corner, top right, tidied (*Stated* 2026-11-11, near the top of the list): no more "Signed in
  as ... Profile, Sign out, Delete account" laid out in a row. It shows one of three things:
  - to a player not yet known, one clear call to say who they are, which leads either to an alias for the session
    or to signing in;
  - to a player playing anonymously, the alias taken for the session: always unique, never the same as another
    anonymous player's at the same time, nor as any username a signed-up player has chosen;
  - to a signed-in player, their username, with a down arrow beside it opening menus and dialogs to add identity
    providers, change what their profile shares, sign out, or delete their account.

  *Done*: the corner shows **Sign in, or choose a name**; a guest's alias, with "(guest)"; or the player's
  username with a ▾ opening **Profile and sign-in services**, **Sign out** and **Delete account**. The server
  holds each guest's alias for twelve hours from when the page last asked after it (*Stated* 2026-11-12),
  offering "Ada 2" when "Ada" is taken ([http-api.md](bots/http-api.md#a-guests-name-for-the-visit)). A guest
  claiming a seat sends their alias token too, and a guest's name that a player goes by, or another guest holds,
  is refused (*Proposed*): no guest can sit under another's alias or a player's username. A name no one holds is
  anyone's, so a program playing as a guest needs no alias (*Stated* 2026-11-12).
- *(Stated 2026-11-12)* A player's card: clicking a player's name shows what is useful to know of them, their Elo
  and experience among it. *Done* in part: the dialog a name opens, at the table or on the leaderboard, adds how
  they stand at the game, from its leaderboard: Elo, experience, and games won, lost and drawn there; or that they
  are not on it yet; and, since 2026-11-13, their three winning streaks there (of every game, of rated games, of
  games against people), each with the mean Elo it beat, a row each. More may follow (their other games).
- **N2** The database stays as it is through every change, by migrations; never wiped (*Stated*).
- **N3** A leaderboard, for players to compete for; with each player's Elo and their **experience**: each time
  a player is on the winning team, they gain their own points plus their team's (*Stated*), changed
  (*Stated 2026-11-12*, as a stronger ally gave more): their own points earned, and as much again times their
  share of their team's, so that carrying a weak ally gains more than being carried by a strong one; alone, it
  is twice their points, as before (`StandingService.ExperienceOf`; past games recounted by the migration
  `RecountExperience`). Each player's
  statistics also count the games they won, lost, drew and abandoned (*Stated*). *Done*: the **Leaderboard**,
  a panel of the lobby, lists, game by game (opening at the game last started), the signed-in players by Elo,
  with their experience, points and
  games won, lost, drawn and abandoned. A game is abandoned by a player while it is not over, has no guest at it
  (such games are purged instead), has been untouched for 24 hours, and waits for that player's own move; waiting
  for someone else's, it counts for no one, and once the player moves it counts no more (*Stated* 2026-11-06).
  Each finished game counts once, within a minute of ending (within seconds, since 2026-11-19), for every
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
  (the backlog's developers' section). UTCP: *Done*: the server serves a [UTCP manual](bots/utcp.md) at `utcp`
  (the MCP tools, by the same names, each a call to its HTTP route), and
  [UtcpSampleBot](../samples/UtcpSampleBot/README.md) plays by it, with a small UTCP client of its own, as there is
  no C# one yet.
- Grand Legions (preview until these are done):
  - **N25** Letters as pieces, ambiguous ones, make it nearly unplayable: it needs its own pieces' symbols.
    *Done*, as asked: every piece has a symbol of its own, as chess's pieces have theirs (King, Queen, Rook,
    Bishop, Knight), and the elephants the elephant; like pieces have like symbols (Stated; see
    [spec §8](grand/specification.md#8-the-page)). A class's emblem with letters, as first done, was not
    playable alone; the class's emblem is now drawn small beneath the symbol, so that the class is told too, with
    every piece's own abbreviation beside it (Stated). The emblems are pictures, not a map's frames, which were too
    alike and abstract to tell apart (Stated 2026-11-06): a flag, crossed swords, a stirrup, a weight, a bomb; the
    Courtier a plumed cap (Proposed). Rivers wave, rather than run straight. Still to come (in the backlog): lifts
    as small badges above the symbol, one per kind of restriction, once a piece's lifted restrictions are kept in
    the game.
  - Each game's pieces priced on its own board, from its actual dimensions, not values set per size (*Stated*
    2026-11-06; [pieces](grand/pieces.md#prices-for-each-game)): on the small board the Giraffe cannot move
    from the centre, yet costs what it does on 8 × 8. *Done*: reach counted from the centre of the actual
    board, and the promotion premium over the actual distance, adjust the sheet's value.
- The piece cards (*Stated* 2026-11-06): a **Cards** setting at the table turns them off and on, remembered;
  and a card may be dragged by its title anywhere on the screen, where it opens from then on. *Done*. The lobby
  says, under Quick game and Custom, that Custom is how to play other people.

## Seeing what happens

- **25** In the lobby, say when each game started (who plays, how many moves and which seats are
  open are shown). *Done*: each game says when it was created and last changed ("created 3 h ago, last change
  5 min ago").
- **Scenario games on a development server** (asked 2026-11-19): an optional filter hiding them from **Waiting for
  your move** and the games list; and a way to be rid of them, a button deleting them all, or each deleted as the
  player leaves it. *Done*: where the server offers scenarios, the lobby offers **Hide games started from
  scenarios**, hiding them from both lists and remembered with the list's other filters, and **Delete every
  scenario game**, which lets go of them all at once (*Proposed*: the button, not deleting each as it is left,
  which would lose a game the player meant to come back to). A server offering no scenarios refuses the deletion
  as forbidden. Games are told apart by their first action, so no column was added to the database.

## Rules, as asked in play

- **Swap or capture** (asked 2026-11-19): with Safety off, a move onto an ally that could also swap with it asks
  "Swap or capture?" rather than assuming the swap. Needs the rules to offer capturing a swappable ally as an
  action of its own, beside the swap. *Done*: where a piece back to back with an ally's may also take it (while an
  enemy leader holds that ally's throne, and the piece takes straight ahead), the rules offer both, the capture a
  move that says so (`captures`, said only then, so every move recorded before means what it did). With Safety
  off, the page asks **Swap or capture?**, with the swap's own label (green) and **Capture it** (red, 2026-11-20:
  both had been dark buttons on a dark panel); with it on, the capture is
  kept back as any taking of the team's pieces is, and the swap is made at once.
- **Dice off** (asked 2026-11-19): a game's creator may turn the dice off, in Chaturaji 2027 or Grand Legions,
  making it a pure abstract strategy game. *Done*, as accepted: a lobby checkbox, **Play without dice**, shown for
  any ruleset that rolls dice (`HasDice`), remembered as last set, and kept by Play again. The setting is recorded
  in the game's first action (`dice-off`, on a deal, or on a `setup` action of its own for a game not dealt), so
  the game replays as played; a game from a scenario keeps the scenario's dice. The state says it
  (`AreDiceOff`), and the rules ask that one place: no roll-off (its opener opens, see below), no
  roll offered at a turn's start (while an enemy leader holds the seat's throne, a pass instead of the roll), so
  no doubles and no escapes; a game still drafting opens so once drafted. The games list marks such games **No
  dice**; the guide and the AI tools' description say what changes; bots need no telling, since they choose among
  the legal actions. Players and bots are rated apart, under the game's id plus `-without-dice`; a bot's Elo
  shown there is its rating at the game's sizes.
- **21, 22, 26** (rest) The idle-rounds safety net as a per-game setting. *Done* (2026-11-19): in a custom game,
  **All-pass rounds to end** (named **Idle rounds that end the game on points** until 2026-11-20, which was
  unclear; the whole told on hovering) takes any number of 1 or more, the server's own (3) shown until one
  is typed, remembered as last set and kept by Play again. It is recorded in the game's first action
  (`idle-rounds`, on a deal or a `setup` action), so the game replays as played, and the rules count to it in place
  of the server's own; a game from a scenario keeps the server's.
- **Dice off: who opens** (asked 2026-11-19): not always Black, which would favour a creator playing Black. *Done*:
  under **Play without dice**, **Who opens** offers *At random* (the default) or any seat by its colour, remembered
  as last set. The server draws a random opener when the game is made, by its own dice, and records the seat in the
  game's first action (`opener`), so the game replays as played; the rules open with it (`GameState.Opener`), a
  drafted game once its draft is done. Play again draws anew. *(Stated)* While an enemy leader holds a seat's throne,
  a game without dice passes instead of rolling: acceptable for now, to be playtested.
- **Bots' kinds on the leaderboard** (asked 2026-11-19). *Done*: the board of players and bots together marks 🧑 a
  person, 🤖 a bot; a bot playing by an AI, as its profile says (`UsesAi`, now on each bot's standing too), the same
  robot tinted teal (2026-11-20: a brain icon read as a blob, unlike the others); today Athena and Pythia. No key, in
  words or icons: each icon in the list names itself on hover.

## Look and feel

- **The leaderboard reshaped** (2026-11-20: the lobby jumped about at each choice; lopsided; columns far too wide).
  Games and leaderboard each hold half the lobby, locked. A board or rating chosen is shown from what the server
  already gave, nothing asked again, so nothing on the page moves. Board and Rating sit on one line; the first column
  is "Rating" whatever the system. Every number's heading reads upward, so each column is as wide as its figures; a
  streak's mean Elo beaten sits under its count. Only the table scrolls, if it must, never the panel's text.
- **Dice audited** (2026-11-20: "Sometimes it seems like the bots, especially Sage, get lucky rolls far too often").
  `scripts/dice-audit.py` tallies every face rolled in the finished games by who rolled it: over 129 games, Sage's
  2,949 faces were the most even of anyone's, its doubles below one in six. Bots never roll: the server rolls for
  every seat.
- **Games no one watches no longer wait on a server bot** (2026-11-20: "does it really make sense for the game to be
  waiting for a bot?"). A server job plays on any game whose next seat is one of the server's own bots and that has
  sat unchanged for 15 seconds, until a person (or an AI over the API) must decide. A game someone watches still
  moves at their table's pace, a step at a time.
- **Every game's board, kingdoms and budget listed** (2026-11-20: "the viewer still would not know what any of these
  values are"). The lobby's Board, Kingdom and Budget columns show whenever they are known, alike or not; the Size
  column whenever the game comes in more than one size.
- **The rating choice on every leaderboard board, and on bots' profiles** (2026-11-20). Bots carry Glicko-2 and
  TrueSkill ratings from their Elo, as people's games against them count them.
- **Elo, Glicko-2 and TrueSkill side by side** (2026-11-20: "Could we calculate ELO, TrueSkill, and Glicko-2
  values? Players could then toggle which to view"). *Done in part*: each person's three ratings are counted from
  the same games, every past game counted again by a migration; the People board's **Rating** choice shows one,
  remembered; a profile shows all three, each with how unsure it is. Glicko-2 rates a team game by each team's mean
  against the other's; TrueSkill as published, two sides of summed skills, shown as the mean less three deviations.
- **D2 Bots for the very first games** (2026-11-20). *Begun*: Morpheus, Phaethon and Tantalus, Random but for
  one turn in 50, 20 and 10 played as Midas; the overnight tournament and the server's job rate them.
- **D4 Strong bots by family** (2026-11-20). *Done in part*: eight bots, one per family (Philidor, Sun Tzu,
  Yi Sun-sin, Musashi, Naresuan, Palamedes, Ragnar, Oberon), play as Sage and draft their family's pieces while
  they can afford any; the server's rating job rates them as it does any new bot.
- **Grand Legions out of preview** (2026-11-20: "if/as/when there is nothing left but UI enhancements, bot
  enhancements, rating enhancements, and minor bug fixes ... we can take GL out of preview"). *Done*: every size
  is listed as any game is; what is left of it (the backlog's sections 3 to 5) adds to it rather than completes it.
- **Board, kingdoms and budget told** (2026-11-20). *Done*: the New game dialog shows a chosen size's board and
  kingdoms, width and height, as it does a board set by hand; the games list, where games differ in size, gives
  each its **Board** and **Kingdom** ("8x8") and its **Budget**, the points each army has to spend on its pieces.
- **Leaderboard: People, and no gaps** (2026-11-20: "the term "Players" is misleading because bots are players";
  "some of the columns seem insanely wide"). *Done*: the people's board is called **People** (its names column
  **Person**; everyone's **Person or bot**); the board's columns stand as close as their figures allow, and its
  panel is as wide as its board and no wider, up to half the lobby, the games taking the rest. Each board keeps
  its own columns: a bot has no experience or abandoned games, a person no games "against people" apart.
- The New game dialog's river choices sit in the armies' grid, each name beside its list.
- **Restriction or weakness** (asked 2026-11-19). *Done*: a piece's card lists its **Restrictions**, which the
  game's rules impose and a lift may remove (its palace, a river, a path that must be clear), apart from its
  **Weaknesses**, its own move's limits, which nothing lifts (one colour of square, one cell in nine)
  (`PieceGuide.Weaknesses`).
- **Rivers as chosen** (asked 2026-11-19). *Done*: in a custom Grand Legions game the creator chooses the rivers
  (none, ↕ or ↔ bisecting the board, or ✚ quartering it) and how they flow (through the kingdoms or around them),
  each left "At random" unless chosen. The rulesets say what of the terrain may be chosen
  (`IRulesApplying.TerrainAxes`), so the lobby draws one list per axis from that data; the choice is kept in the
  game's first action, so a game replays, and is played again, on the same rivers; and the lobby remembers it.
- **A turn that changes nothing is a pass** (2026-11-20: "It is effectively a pass and should be treated as a
  pass whenever a pass matters"). *Done*: the rules remember how each turn began (`GameState.TurnStart`); a turn
  ending with the board and the prisoners as it began counts toward the rounds of everyone passing, on from the
  passes before it, whatever dice it used. In place, for every game.
- **Shogi leaders are mercenaries too** (asked 2026-11-19). *Done*: every Shogi piece, the leaders an army starts
  with included (a Gold General, say), is a mercenary: it bears the gold coin, and whoever holds it prisoner may
  hire it. The rule changed in place, for every game.

- **A mercenary marked on the board** (asked 2026-11-20). *Done*: the rules say of each piece whether it is a
  mercenary (`IRulesApplying.IsMercenary`), by its kind or as what a mercenary was promoted into (a Shogi Pawn
  become a Western Rook); the page is told so of each piece (`PieceDto.IsMercenary`), draws a gold coin on its
  shoulder, and lists it on its card among its drawbacks.

- **Itch frame** (asked 2026-11-19): in itch.io's frame (about 1040 by 630), the New game dialog opened over
  everything, and the games and the leaderboard each got half of a width too small for either. *Done*: in any frame,
  New game starts closed until asked for (the player's own choice still rules after). The lobby is sized by the room
  it has, in a frame or on a device alike: under 1150 wide, the games and the leaderboard each take the whole
  width, the leaderboard below; under 720 high, New game floats from the top and takes all the height; under 640
  wide (a phone), panels fill the screen and each choice's name stands above its choices. The table already fits
  its window by its own widths.

- *(Stated 2026-11-12)* A piece card makes plain how each move is made. *Done*: a square the piece leaps to, over
  anything between (a Knight's move, say), has a hollow dot, a ring, and the key says so; any other dot farther
  off, it slides to, up to the first piece in the way; a step to the next square is neither. The Shogi Pawn
  takes the piece directly ahead, as it moves, as in Shogi: it is no longer said to be blocked straight ahead
  (the pieces spreadsheet too). The Dragon Horse (Shogi) is a Bishop that may also step one square
  orthogonally: never a Knight's leap, despite its name, as in Shogi.
- **3** A choice between classic chaturanga pieces and "modern" ones (the elephant is effectively
  a rook): the pieces show as traditional (Raja, Elephant, Horse as silhouettes) or chess (King, Rook, Knight),
  as the player chooses (below, after the second playtest).

## Rules

- Ransom (§8): bots that offer during their turn. Worth doing (*Stated 2026-11-07*): offered at the start or in
  the middle of a turn, a trade may bring back a piece of one's own to where it can take something at once, or
  send an enemy's home to where it can be taken at once; and a trade made before moving gives less away of how
  the player means to use what comes back. *Done*: Midas, Solomon, Argus, Sage and Tactician offer whenever the
  rules let them, the first trade that suits them, one a turn; each still weighs the trade by the position once
  it is done, not by the moves it opens this turn (*Proposed*).
- Bots should weigh where a ransomed piece lands, not only what it is worth: it may come back between
  a rook and the piece it guarded or attacked. *Done*: Argus weighs a trade by what an enemy could take once it
  is done, Sage and Athena by the whole position then, and every bot refuses one that pens a piece of its team
  in ([ai-assessment.md](bots/ai-assessment.md)).

## Players and identity

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
  the server's bots, by Elo (each bot's rating from games between the bots, the mean of its ratings at the game's
  sizes), each seat a bot played counting, with their record against people alone. **Everyone** *(Stated
  2026-11-11: not how people fare against bots, which the first try counted, but one board that does not care
  whether a row is a person or a bot)* lists players and bots together, by Elo, each marked 🧑 or 🤖. Counted
  from 2026-11-07, not for games before.
- The leaderboard a panel of the lobby, not a button in the bar (*Stated 2026-11-11*). *Done*: beside New game
  and Games.
- Every leaderboard sorts by any of its columns, its header clicked (Elo, experience, won, lost, drawn,
  abandoned), and shows ten at a time, paging for more (*Stated 2026-11-07*). *Done* (*Proposed*: how): a header
  clicked sorts most first, clicked again fewest first; the server sends up to 1000 of each board, and the page
  sorts and pages them itself (a larger population would want the server to page instead). Points are shown on
  every board (*Stated 2026-11-11*), explained under the board and in the column's header: the points each scored
  itself, in every game finished, won or lost.
- The names on the leaderboards open the player's, or the bot's, profile (*Stated 2026-11-07*). *Done*: as a
  name at a table does.
- A player's profile filled out (*Stated 2026-11-07*): a description of their own, and a choice of which of the
  services they sign in with others may see. *Done* (*Proposed*: how): the profile dialog has **About you**, up to
  500 characters, line breaks kept, and a tick beside each service the player signs in with, to show it; none is
  shown until ticked (`PUT api/players/me/profile`). A name at a table or on a leaderboard opens the description
  and the services shown.
- A player's username unique (*Stated 2026-11-07*): no player may take a name another has. The profile's field
  is called **Username**. *Done*: a rename to a name another player goes by, capitals or not, is refused (`409`)
  and the profile says why. A new player whose service's name is taken gets the first of "Ada 2", "Ada 3" and so
  on that is free (*Proposed*); names shared before this stay as they were (*Proposed*).
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
  custom games (*Stated 2026-11-07*). *Done* (2026-11-12): Grand Legions offers a **custom** size, whose board
  (6 to 26 cells) and kingdoms (width and depth, at least 3 × 3, not overlapping) the creator sets, the
  kingdoms' size too (*Stated*). The size is kept in the game's first action and the rules are rebuilt for it on
  every replay, under the one ruleset id `grand-legions-custom`, so its games are listed and rated together. Each
  army's budget is 12 points a kingdom cell, and its minimum army grows with the kingdom (*Proposed*, see
  `QuickGame.CustomOf`). *Done* (2026-11-13): the board need not be square (*Stated*: "Boards don't need to be
  square!"), its width and height set apart, in a table with the kingdom's, the board and the kingdom its rows,
  the width and the height its columns (*Stated*). Each seat's army stands in its own corner, facing across the
  board or up it (`Pinwheel.At`); a game set before keeps its square board. A palace that may stand partly outside
  a kingdom too small for one stands only where it may for every seat (*Proposed*), and, from edition 14
  (2026-11-19), without any square another army's could stand on too, so that no two overlap and every army keeps
  one (*Stated*). Bots are rated as at the game's own size nearest in width and
  height together (*Proposed*).
- A custom game's pieces chosen by hand and placed into each kingdom, rather than only dealt (*Stated
  2026-11-07*): the other of the two most noticeable gaps. *Done* by the army draft, in turn or in secret, with
  thrones and palaces placed by each player ([roadmap](grand/roadmap.md), Stage 4); what is left of the draft is in
  the backlog.
- The page's preferences (theme, sound, pieces, the log's highlights) kept with the player's identity
  once there is one, not only in the browser. *Done*: a player signed in keeps the theme, sound, safety,
  pieces, piece cards, log, the talk's order and AI talk, previews and how the lobby sets games up with the
  server as they choose them (`PUT api/players/me/preferences`), and every browser they sign in on takes them
  up; seats held, a game's own setup and where the piece card was dragged stay with the browser (*Proposed*).

## Bots

- **No pass with extra steps** (noticed 2026-11-20: a bot's Rook c2 to d2 and back, taking nothing). *Done*: every
  bot, the server's and self-play's alike, is offered every legal action but one that would put the board and the
  prisoners back as they were when its turn began, unless nothing else is left; so it passes, or does something.
- *(Stated 2026-10-26)* A bot that learns from past games and analyzes the current one.
- *(Stated 2026-10-29)* Players can play against an AI, and the claim must mean something: a bot that
  has learned the game, measured against the hand-tuned ones, not a language model picking among
  moves it does not understand. The scores and how a game ended (agreement, one team left, a team
  giving up...) matter to what it learns, not only who won.
- *(Stated 2026-10-29)* Pythia (Sage asking a free AI service which of its three best moves to make)
  was removed: it added lag and spent quota without a reason to think it played better. It is kept
  in the repository's history, at the tag `archive/pythia`. The name now belongs to a different bot (2026-11-12,
  *Proposed*): Monte Carlo tree search judged by Athena's network, with no AI service
  ([assessment](bots/ai-assessment.md#progress-2026-11-12)).
- *(Stated 2026-11-12)* **Weak bots, for beginners.** The bots have grown strong enough that an "easy game" is no
  longer easy: Random is the only one left that is truly weak. A few more weak bots, somewhere between 800 and 1200
  Elo, would let a beginner enjoy a game, not just lose it. Their details are mine to choose. *Proposed*:
  - a bot that takes whatever it can, the most valuable first, and else moves at random;
  - one that plays one of its top few moves at random, by Sage's or Athena's judgement, with how often it errs
    as its strength;
  - one that looks no further than its own move, missing what the enemy can take back.

  Each is rated by the bot-rating job like the rest, so the picker can show which are within reach.

  *Done* in part (2026-11-12): **Echo** and **Icarus**, which play as Midas one decision in four and one in two,
  and as Random the rest. The server's rating job rates them; if they land too near Random or Midas, their shares
  are what to change. The rest is the backlog's D2.
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
     - Next (in the backlog): command and empires (thrones and leaders weigh them only in part), and the
       enemy's reply. The Chaturaji 2027 features serve the grand game too (grand/specification.md §6.1).
     - Done *(Stated 2026-11-05; the split Proposed)*: every weight but exposure and chances is told
       apart by side: the bot's own seat's, its ally's and its enemies'. A single number in the
       weights' JSON still weighs all three alike, so weights tuned before keep playing as they did;
       tuning nudges each side on its own.
     - *(Stated 2026-11-05)* Pieces decomposed: a piece's worth comes from its reach, its
       restrictions, how fast it moves, whether it can go back, and how near it is to promoting. Those
       parts, measured where the piece stands (little reach at the edge, a Pawn a step from promoting),
       may say more than the piece's fixed worth. *(Proposed)*: count them as measures of their own.
       Done *(Proposed: each measure)*: beside the Pawns' progress, Chaturaji 2027 and the grand game now
       measure how near each army's Pawns are to promoting (one over the rows each has still to go), its
       pieces' reach where they stand (their moves, all told), and how many of those moves go back. They
       start at no weight, for tuning to find theirs; Athena's network, trained before them, plays on
       seeing only the measures it knew.
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
       its enemies, starting at no weight for tuning to find.
     - *(Stated 2026-11-06)* Along the same line: giving up the leader, so that an ally commands the
       army; and passing up a ransom or an escape that would bring the leader home, so that the ally
       keeps command of it. Probably worth more than handing a turn over, unless the ally is the
       stronger player (higher Elo). *(Proposed)*: learned rather than written in. Sage already weighs a
       ransom and an escape by the whole position once it is done, so `commanded`, weighed for the
       ally, counts what bringing a leader home takes from the ally; tuning finds how much. Giving the
       leader up on purpose needs Sage to look past the enemy's taking it, which it does not yet do; the
       ally's rating is for the bot to weigh as trusting the ally says. Midas, Solomon and Argus
       weigh a ransom by worth alone, and keep doing so.
     - *(Stated 2026-11-06)* Pieces in an enemy's kingdom, and above all on its throne, stand where
       escaping prisoners land (rules §7), so entering and sitting there is a risk. Done *(Proposed)*:
       a fourth measure, `under-escapes`, what a seat's pieces are worth that stand on the starting
       square of an enemy's piece now held prisoner, by anyone; weighed apart by side, starting at no
       weight. Only prisoners held now count; a square some later prisoner might land on does not.
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
       swapped halfway, to learn from in the next (`scripts/athena-rounds.py`; since 2026-11-19,
       `scripts/wsl-overnight.sh` to leave it learning unattended). It is called stronger than Sage only at
       55% or more of 400 games.
  4. **Bring your own AI** (*Stated 2026-10-29*): players connecting their own AI, to play and to
     train it: through MCP (already possible, `Zaturnava.Mcp`) and the HTTP API; recorded games to
     download; and a self-play kit (BotRunner, with a bot of one's own). Documented for developers,
     and reachable from the game itself.
     - Done: [docs/bots/](bots/README.md), for developers: MCP, the HTTP API, game histories, and
       self-play, the BotRunner seating bots from an assembly of one's own (`--bot-assembly`, also
       taken by `rate-bots.py`). "How to play" links to it, on GitHub ("The bots" did too, until it was removed,
       2026-11-18).
     - Done: [a sample bot over the HTTP API](../samples/HttpApiSampleBot/README.md), standing alone, mechanical,
       playing the live server through the HTTP API, with notes on putting an AI in it.
     - Done: [a sample bot over MCP](../samples/McpSampleBot/README.md), its twin: the same loop and
       mind, through the server's MCP tools, by the protocol's C# SDK.
     - Done: [a UTCP manual](bots/utcp.md) at `utcp`, and [a sample bot over UTCP](../samples/UtcpSampleBot/README.md)
       playing by it.
- *(Stated 2026-10-29)* Once the game is online, free AI bots are offered for players to play
  against, subject to whatever policies their hosts require.
- **A bot using Claude**: shelved (*Stated 2026-11-01*). No one will pay for its tokens: not the owner, and
  players are unlikely to give their own key. Without a trained instance to call, it would be an expensive, slow
  random player. The plumbing for a bot paid for each use (`BotProfile.IsPaid`) stays, unused. An AI of a
  player's own joins instead: in a custom game, a seat may be left for "Your own AI", which takes it by MCP or
  the HTTP API with an AI token, and the table shows the game's id while seats are open.
- *(Stated 2026-11-12)* Bots budget their drafted armies badly: on a large board they averaged about 8 pieces a
  kingdom. A few strong pieces can be a fair plan, and some bots may lean that way, but not every bot every time.
  *(Proposed)*: each bot drafts by a plan of its own (many cheap pieces, a few dear ones, a balance across the
  classes), rather than all following the random fill. Ideally *(Stated 2026-11-12)*, in an open draft, the
  stronger bots look at what the others are drafting, and choose and place their pieces in answer.
  *(Stated 2026-11-12)* Given a large budget, bots bought cheap pieces rather than use it. *Done (Proposed)*:
  a bot still follows the random fill, but on any square but its throne it may buy a stronger piece there,
  one it can pay for while keeping enough for the rest of that army. *(Stated 2026-11-12)*: price is not
  strength (a Shogi Pawn is dear for the leader it may become), and bots all buying alike leaves little variety.
  So strength is the piece's reach, the squares its card shows it moving to or capturing on, and the bot draws
  at random among the pieces it can pay for within 75% of the strongest's reach (`FillingArmies`). Each bot's
  own plan is the backlog's E4.
- *(Stated 2026-11-12)* Rate the bots not yet rated, in the background, without a command anyone has to
  approve: every bot, over a good mix of rulesets, for as long as it runs, each result saved as it comes, so
  that whatever was gathered counts however and whenever the run stops. *(Proposed)*: a job the server runs
  itself, when its settings turn it on, playing the bots against each other and keeping each game's ratings as
  it ends, rather than `scripts/wsl-rate.sh`. *Done*: see [the rating job](bots/self-play.md#the-servers-rating-job).
- *(Stated 2026-11-12)* Midas, at Green, blundered a piece: Black's Rook attacked a Pawn with Red's Raja
  behind it, so only a double Rook roll could take both, and Green moved a Horse between them, shielding
  Red's Raja but leaving the Horse where that Raja could take it for nothing, without making any threat of
  its own. Greed does not explain the move. *Found*: Midas, as written, takes the action that scores most at
  once and, when none scores, moves at random, never looking past its own action; the block was chance, not
  a choice. *Stated 2026-11-13* (was OQ11): Midas stays as careless as it is; a less careless Midas, one that
  when nothing scores prefers a move leaving none of its own pieces newly open to capture, might be an
  additional bot. Not a priority. *Done* (2026-11-13): **Croesus**, Midas but for that; "newly open" *(Proposed)*
  is the team no more exposed, as Argus weighs the worst an enemy could do in reply, than before the move. The
  server's rating job rates it.
- *(Stated 2026-11-12)* The leaderboard shows each player's winning streak, the games won since they last lost,
  three ways: over every game, over the games that count toward Elo only, and over the games against people
  only; each with the mean Elo of the opponents beaten in it. *Done*: the players' board has **Streak**,
  **Rated streak** and **People streak** columns, each with "vs" and the mean Elo of the opposing teams beaten.
  *(Proposed)*: a draw neither adds to a streak nor ends it; against people means every seat not on the
  player's team was played by a person, signed in or a guest; merged players keep the longer of each streak.
  Every standing was counted again from the games themselves, so the streaks start from the whole history.

## Talking

- **27, 28** Chat, to everyone or to chosen players (a teammate). *Done*: beside the log, the table's talk has
  a **Table** tab, said to everyone at the table, and, for a player, a **Team** tab, said to their team alone;
  what AIs say can be hidden.

## Done, as the backlog listed it

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
- "The bots" opened beside it (removed 2026-11-18: a bot's name opens its profile): each bot, easiest first, with
  its Elo rating at the ruleset, from games between every pair of them (`scripts/rate-bots.py`, Random at 1000).
  The lobby's quick game is easy (the three easiest bots), medium (any three) or hard (the three hardest).
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
