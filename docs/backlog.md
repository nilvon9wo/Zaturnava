# Backlog

What is still to be done, sorted by kind. Numbers are the playtests' own, so that each item can be traced to the
note it came from. What has been done, and the choices made doing it, is in [done.md](done.md); an item moves
there with the change that finishes it. Questions waiting on an answer are gathered in
[open-questions.md](open-questions.md). The playtest of 2026-11-15 is tracked apart, in
[playtesting/traceability.md](playtesting/traceability.md); the grand game's stages in its
[roadmap](grand/roadmap.md).

## Players and identity

- A player's card (*Stated* 2026-11-12): beyond their standing at the game, which it shows, their other games.
- **29** One person, one identity: a person may play several seats of one team when the game allows it, without
  pretending to be several people.

## Look and feel

- Grand Legions' lifts (**N25**): as small badges above a piece's symbol, one per kind of restriction lifted, once
  a piece's lifted restrictions are kept in the game.
- *(Stated 2026-11-12, low priority, a nice-to-have)* The table's panels (the draft's list of pieces, the turn's
  panel, the log and talk) resized and placed by each player as they like, as a piece card already may be dragged.
- **2** Pieces drawn as pictures of their own; for now they are chess glyphs, and the boat a drawn sailing boat.
- **N29** (low) An encyclopedia of pieces: each piece's name here, its other names, the games it comes from
  (Chess, Xiangqi...), the games here that use it, its variants (kinds of Pawn, of Elephant...), and how it
  moves, captures and what else it may do.

## Rules not yet played

- **21, 22, 26** (rest) The idle-rounds safety net as a per-game setting.
- Boat triumph (§9): a nice-to-have; the condition is so unlikely that no one may miss it.
- *(Stated 2026-11-12)* **Kingdoms placed by the players**, as a setting of the game's creator: today every kingdom
  stands in a corner of the board, whatever its size; with it, each player would place their kingdom before their
  palace, throne and pieces. How free to make it is still to be worked out. Placing it anywhere could leave a later
  player no room for theirs, so kingdoms should probably still keep to the board's corners, more or less.
- **3** Variant pieces: two Chinese-chess-like elephants, one confined by two "rivers" that divide the board in
  four, and a dearer one that may cross them; the boat is to be replaced.

## Bots

- *(Stated 2026-11-12)* More weak bots, for beginners, between 800 and 1200 Elo; Echo, Icarus and Croesus are done
  ([done](done.md#bots)). *Proposed*, still to try: one that plays one of its top few moves at random, by Sage's or
  Athena's judgement, with how often it errs as its strength; one that looks no further than its own move, missing
  what the enemy can take back. Each is rated by the bot-rating job like the rest.
- Sage, next: command and empires (thrones and leaders weigh them only in part), and the enemy's reply. With the
  enemy's reply weighed, Sage may take its own team's pieces (rules §6.4) when that saves a better one
  (*Stated* 2026-10-29); until then no bot does.
- Sage's newer measures (the pieces' promotion, reach and retreat; leaders attacked, thrones in danger, armies
  commanded; `under-escapes`) start at no weight: tune them, and teach Athena them.
- *(Stated 2026-11-05)* Trusting the ally: how far to rely on the ally's pieces should depend on how well the ally
  plays. *(Proposed)*: the ally's rating as a feature, so the ally-side weights can grow with a strong ally and
  shrink with a weak one; learned by Athena, or tuned in Sage against allies of several strengths.
- *(Stated 2026-11-05, 2026-11-06)* Shared command: a bot that hands its turn to a regent ally (no bot does yet)
  when the ally would play the rest of it better; giving up the leader on purpose, so that an ally commands the
  army, which needs Sage to look past the enemy's taking it; passing up a ransom or an escape that would bring the
  leader home, so that the ally keeps command. *(Proposed)*: learned rather than written in.
- *(Stated 2026-11-05)* Private team chat, and how well bots plan through it; moot while our bots do not talk, and
  not with bots that spend tokens.
- Athena, then: the kind of ending as a third prediction, so that agreeing to end becomes a move it can judge, and
  the board square by square, turned to each seat's own view, once there are hundreds of thousands of games. People's
  games mixed in as they accumulate (*Proposed*).
- Bring your own AI, next: a push channel, so that a program need not poll; the engine and the bot interface as
  packages, so that a bot need not reference the repository's projects (*Proposed*: Athena's value network among
  them, so that the sample bots, moved to repositories of their own, may play by it); replaying a history without
  .NET.
- Drafting (*Stated* 2026-11-12): each bot drafting by a plan of its own (many cheap pieces, a few dear ones, a
  balance across the classes), rather than all following the random fill; ideally, in an open draft, the stronger
  bots answering what the others are drafting. Later, once the game is more settled: bots of their own leanings,
  such as one preferring pieces of one origin (Western, Shogi...), or sliders, or infantry; an army of different
  bots to play against.
- *(Stated 2026-11-12)* Fairy-Stockfish as a bot. *Begun*: it plays, see
  [Fairy-Stockfish, as a bot](bots/self-play.md#fairy-stockfish-as-a-bot); its rating is still to come. It advises
  one seat at a time, its pieces in Betza notation, limited to the seat's legal moves for the roll, in an adapter
  project of its own, so its GPL-3 licence stays with it; in the BotRunner only, before any thought of hosting it.
- *(Stated 2026-11-12)* Evaluate an OpenSpiel adapter. *(Proposed)*: DeepMind's OpenSpiel, whose games may have any
  number of players and chance (the dice), and whose algorithms (MCTS, AlphaZero-style self-play, CFR) could make
  bots without Fairy-Stockfish's blind spots. It is C++ and Python: weigh porting the rules against a bridge to
  ours. An internal tool for bots, not a public interface.
- *(Stated 2026-11-12, not a priority)* Fold into Grand Legions the pieces Fairy-Stockfish's built-in variants have
  that it lacks: list each piece not already in `docs/Other Potential Pieces.xlsx` or the game, and add them there
  and to the game, each with its own symbol.
- A mechanical bot that likes to roll, rather than always moving without dice (not a priority).

## Later

- *(Stated 2026-11-12, not a priority)* Upgrade to .NET 11, once MonsterASP hosts it (itch.io serves only the
  page's static files, so its side is the WebAssembly build). The SDK CI and the deploys build with is pinned
  (`scripts/pin-sdk.py`, and each workflow's setup-dotnet version): moving to a newer SDK means moving the pin.
- *(Stated 2026-10-31, low priority, an idea)* **Games made by others, as data alone.** Anyone may describe a game
  of their own (its board, pieces, how each moves, the dice, the setup, how it is scored and ends) in JSON or XML,
  and play it on the live server. No one may send code: every structure, movement and rule a description names
  must already exist in the engine. *(Proposed)* What it would take, roughly:
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
