# Open questions

Every question waiting on the designer's answer, in one place. When one is answered, the answer goes where it
belongs (the rules, the specification, the roadmap, the code), marked *Stated* there; the question here is then
cut down to whatever is still open, or removed if nothing is. Each says where its answer will go.

Choices made without an answer are marked *Proposed* where they are written, and stand until answered otherwise.
The ones most worth a look are listed under [Proposed choices to confirm](#proposed-choices-to-confirm).

Answered on 2026-11-06 and folded in: the pieces' symbols, class emblems, abbreviations and lift badges
([spec §8](grand/specification.md#8-the-page)); rivers in quick games
([spec §2](grand/specification.md#2-creating-a-game)); the Boat, the throne, and the safety nets
([rules](rules.md)); every army alike ([roadmap](grand/roadmap.md)).

## Grand Legions rules and set-up

- **OQ7 A second palace** (specification Q5): may a player buy one, on a board large enough, and at what price?
  Left open until the rest of the game is playable.
- **OQ8 Saved settings** (specification Q26), put more plainly: a player saves a set of settings, and others
  play games with it. If its owner then edits the set, should the games already played keep the set as it was
  (each edit a new version), or show it as it now is? And if two players save sets with exactly the same
  settings, should they be kept as two, or merged into one? Left open until players want to repeat games.

## Proposed choices to confirm

Choices of mine already in play, or to be built, each standing until answered otherwise:

- **OQ13 Value review.** *Stated*: the Shogi Pawn's price is intended (it may promote to a leader). Still to
  do (mine): look through the catalogue for other discrepancies between a piece's price and its worth, and
  either make each intentional (noted as a good or bad investment) or fix the formula behind it
  ([pieces.md](grand/pieces.md#still-open)). Findings will be listed here.
- **OQ15 Promotion on entering an enemy kingdom.** The Makruk Pawn's own promotion, "on the sixth rank or
  beyond", is taken as the far three rows on every board (`GrandLegionsRules.OwnPromotions`). The designer
  suggests, if not too complicated, promoting on entering an enemy kingdom instead, which would follow the
  board's and kingdom's sizes. *Proposed*: do so for the Makruk Pawn only; the Western pieces keep the last
  row. Confirm, or should other pieces promote there too?
- **OQ16 When a bot gives up in Grand Legions.** *Stated*: by what is left on the board, not ×10.
  *Proposed*: a team is hopeless when its side has no leader left, or no heavy left and trails, or when the
  points it trails by exceed what its pieces could take in ten rounds: each round, every piece it has may take
  at most the most valuable enemy piece it could reach, counted at no more than one capture per seat per round.
  Confirm, or weigh it otherwise? ([rules §10](rules.md))

Every other *Proposed* mark in the docs ([rules](rules.md), [architecture](architecture.md),
[backlog](backlog.md), [roadmap](grand/roadmap.md), [pieces](grand/pieces.md), the [HTTP API](bots/http-api.md))
stands the same way; ask about any of them here.

## For playtesting

Not questions to answer now, but open until play settles them: the Chaturaji 2027 bonuses in Grand Legions,
×10 to start with, and the leader bonus of 60 (specification Q14); the abbreviations; the lift badges, one per
kind of restriction.
