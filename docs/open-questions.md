# Open questions

Every question waiting on the designer's answer, and only those: there are no others in the other documents.
When one is answered, the answer goes where it belongs (the rules, the specification, the roadmap, the code),
marked *Stated* there, and the question here is cut down to whatever is still open, or removed.

Choices marked *Proposed* elsewhere are decisions made without an answer; they stand as they are and need none.
Anything to be built is in the [backlog](backlog.md) or the [roadmap](grand/roadmap.md), and anything left to
playtesting is marked so where it is written.

## OQ7 A second palace

May a player buy a second palace in Grand Legions, on a board large enough, and at what price? Left open until
the rest of the game is playable ([specification Q5](grand/specification.md#9-open-questions)).

## OQ16 When a bot gives up in Grand Legions

*Stated*: by what is left on the board, weighed together, with where pieces stand
([rules §10](rules.md)). *Proposed* formula:

- **The gap**: how many points the bot's team trails the leading team by.
- **The hope**: what the team could take back within ten rounds, the sum, over each of its pieces, of the most
  valuable enemy piece it could reach, discounted by how far that is: worth × (1 − moves needed ÷ moves it can
  expect in ten rounds, by how often its class is rolled), and nothing if it is farther than that. A leader
  within such reach of an unguarded enemy throne adds, the same way discounted, the worth of the pieces of
  that seat it would come to command.
- **Give up** when the gap is greater than the hope. Having no leader or heavy counts only through the hope:
  such a team has less of it, but plays on if it is enough.

Confirm, or weigh it otherwise (another number of rounds, the hope counted at more or less than its face)?
