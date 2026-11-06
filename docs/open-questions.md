# Open questions

Every question waiting on the designer's answer, in one place. When one is answered, the answer goes where it
belongs (the rules, the specification, the roadmap, the code), marked *Stated* there; the question here is then
cut down to whatever is still open, or removed if nothing is. Each says where its answer will go.

Choices made without an answer are marked *Proposed* where they are written, and stand until answered otherwise.
The ones most worth a look are listed under [Proposed choices to confirm](#proposed-choices-to-confirm).

## Looks of the Grand Legions pieces

- **OQ1 Symbols.** Each piece's symbol (*Proposed*, but for the chess pieces' and the elephants'): Fairy General a
  star; Generals a flag; Gold General the sun; Silver General the moon; Dragons (King, Bishop, Horse) a dragon's
  wings; Guard a shield; Advisor a fan; Minister a scroll; Noble a tiara; Courtier a key; Soldiers a sword; Lance a
  lance with its pennant; Amazon a bow and arrow; Chancellor scales; Archbishop a cross; Knightrider two chevrons;
  Zebra stripes; Camel a camel; Xiangqi Horse a horseshoe; Giraffe a giraffe; Threeleaper three dots; War Engine a
  catapult; Cannons a cannon; Grasshopper a grasshopper. Keep, or which to change? Answer goes to
  [roadmap Stage 7](grand/roadmap.md#stage-7--looks) and `GrandLegionsPieces.Symbols`.
- **OQ2 A picture twice.** The Guard's symbol, a shield, is also Infantry's class emblem; the Cannons' symbol is
  also Artillery's. Each is still told apart by its symbol among its class. Keep, or give them other symbols?
- **OQ3 Class emblems.** A crown for Leader, a shield for Infantry, a battlemented tower for Heavy, a horse's head
  for Cavalry, a cannon for Artillery (*Proposed*). Keep? Answer goes to
  [specification §8](grand/specification.md#8-the-page).
- **OQ4 Abbreviations.** Every piece has its own (the Pawns' Sh, W, M and Soldiers' SJ, SX as asked; the rest
  *Proposed*): DK, FG, K, GX, GJ, Go, Si; Gu, N, Mi, C, A; Am, Q, Ch, Ar, DB, DH, L, R, B; KR, Z, Kn, Ca, HX, EJ,
  Gi, Al, Th, EX, WE; CX, CJ, G. Keep, or which to change? Answer goes to `GrandLegionsPieces.Letters`.
- **OQ5 Lift badges.** Above the symbol, a badge for each restriction lifted. What should they look like: one
  badge per kind of restriction (movement, area, binding), or one per restriction? Not needed until lifts are
  kept in the game.

## Grand Legions rules and set-up

- **OQ6 Rivers in quick games.** *Stated*: decided at random. Still open: which layouts may come up (none, one
  north–south, one east–west, both), and how likely each is; whether a river runs through the kingdoms or around
  them; whether the lobby tells the river before the game starts. *Proposed*: "Play again, same armies" keeps
  the rivers too. Answer goes to [specification §2](grand/specification.md#2-creating-a-game) and the
  [roadmap](grand/roadmap.md).
- **OQ7 A second palace** (specification Q5): may a player buy one, on a board large enough, and at what price?
- **OQ8 Saved settings** (specification Q26): versions, or a check for duplicates? Not pressing.

## Chaturaji 2027 rules

- **OQ9 The Boat's identity** ([rules §13](rules.md#13-open-items--deferred)): keep the Boat, or another
  jumping-diagonal piece (a kangaroo was floated)? Names only; how it moves stays.
- **OQ10 A throne that follows its Raja** ([rules §2](rules.md)): should a throne move with a Raja that starts
  somewhere else? Only matters once a game varies where the Raja starts.
- **OQ11 Safety nets** ([rules §13](rules.md#13-open-items--deferred)): the defaults of `gameEnd.safetyNet.*`
  (a move cap, and so on), and whether the three idle rounds that end a game should be a setting of the game.

## For playtesting

Not questions to answer now, but open until play settles them.

- **OQ12** The Chaturaji 2027 bonuses in Grand Legions, ×10 to start with, and the leader bonus of 60
  (specification Q14).
- **OQ13** The Pawns' promotion premiums may be out of balance: the Shogi Pawn is priced above the Western Pawn,
  though weaker ([pieces.md](grand/pieces.md#still-open)).

## Proposed choices to confirm

Choices of mine already in play, each standing until answered otherwise:

- **OQ14** Every army alike is a checkbox of the custom game set-up, for a dealt game only; a game over offers
  "Play again, same armies" beside "Play again" ([roadmap](grand/roadmap.md)).
- **OQ15** The Makruk Pawn's own promotion, "on the sixth rank or beyond", is taken as the far three rows on every
  board; a Western piece's own promotion is on the last row (`GrandLegionsRules.OwnPromotions`).
- **OQ16** The clear lead a team needs to win by giving up its turns in Grand Legions is 100, the Chaturaji 2027
  lead × 10 (`Scoring/grand-legions.json`).

Every other *Proposed* mark in the docs ([rules](rules.md), [architecture](architecture.md),
[backlog](backlog.md), [roadmap](grand/roadmap.md), [pieces](grand/pieces.md), the [HTTP API](bots/http-api.md))
stands the same way; ask about any of them here.
