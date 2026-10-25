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

- **19** (rest) Voluntary regency (rules §6.2): an ally taking the turn of a seat whose throne it
  holds, or that has no Raja, with that seat's consent.
- **21, 22, 26** (rest) Ending the game by agreement; the idle-rounds safety net as a per-game
  setting.
- **20** Offer to end the game, for the other players to accept.
- Escape on doubles (§7), boat triumph (§9), the empire bonus (§5).
- Ransom (§8): the receiving player choosing among equidistant squares; bots that offer during their turn.
- The commanded-captures toggle (`control.enemyArmyCaptures`) as a per-game setting.

## Players and identity

- **29** One person, one identity: a person may play several seats of one team when the game
  allows it, without pretending to be several people.
- **34** Games survive a server restart (storage that persists, and identities to go with it).
- **35** Player statistics: games played and won, points scored...

## Talking

- **27, 28** Chat, to everyone or to chosen players (a teammate).

## Later

- **3** Variant pieces: two Chinese-chess-like elephants, one confined by two "rivers" that divide
  the board in four, and a dearer one that may cross them; the boat is to be replaced.
- The grand game, of which the classic game is a part: its [specification](grand/specification.md),
  [pieces](grand/pieces.md) and [roadmap](grand/roadmap.md).

## Done

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
