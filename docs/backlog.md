# Backlog

What the first playtest (2026-10-24) asked for, sorted by kind. Numbers are the playtest's own, so
that each item can be traced to the note it came from. Items move to "Done" with the change that
did them.

## Seeing what happens

- **15** Show each seat's prisoners (and, later, the ransom offers they allow).
- **25** In the lobby, say when each game started (who plays, how many moves and which seats are
  open are shown).

## Look and feel

- **2** Pieces drawn as pictures of their own; for now they are chess glyphs.
- **3** A choice between classic chaturanga pieces and "modern" ones (the elephant is effectively
  a rook).

## Rules not yet played

- **30** Roll off for who goes first (rules §11), rather than Black always starting.
- **19** Thrones: a Raja on another army's throne commands that army (rules §6).
- **21, 22, 26** The game must end: when no opponents are left, by agreement, and when every
  remaining player passes without rolling three times in a row (configurable).
- **20** Offer to end the game, for the other players to accept.

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
