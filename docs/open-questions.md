# Open questions

Every question waiting on the designer's answer, and only those: there are no others in the other documents.
When one is answered, the answer goes where it belongs (the rules, the specification, the roadmap, the code),
marked *Stated* there, and the question here is cut down to whatever is still open, or removed.

Choices marked *Proposed* elsewhere are decisions made without an answer; they stand as they are and need none.
They are marked only so that it is known which choices were the designer's and which were not, should one ever
need to change. None is a question in disguise: a choice that cannot stand without the designer's answer is
asked here instead (checked, every *Proposed* in the documents, on 2026-11-06).
Anything to be built is in the [backlog](backlog.md) or the [roadmap](grand/roadmap.md), and anything left to
playtesting is marked so where it is written.

## Proposals to review (asked 2026-11-28)

The designer, 2026-11-28, after the class averages were found leaving out pieces that may promote: "Let me know
if you made any other very questionable 'proposals' which I should review." These are the *Proposed* choices
that change who gets what, or how the game plays. Bot play and looks are left out: bot design is the program's
own call. Each stands until answered: keep it, change it, or drop it. Settled the same day, so not listed: the
price cap on pieces dealt past the aim (dropped), river-hindered pieces left out where there is no river (now
eligible), and dealing buying no lifts (it buys them, after pieces).

| # | Where | The choice | Why it may matter |
|---|---|---|---|
| P4 | `QuickArmy.WeightOf` | Each piece is drawn most often at 80% of its class's median price, rarer either way (twice or half as dear: about three times rarer) | The intent is Stated ("seldom is not never"); the 80% and the rate are mine |
| P5 | `QuickArmy.LeftOverPercent` | A dealt budget is the composition's cost plus a tenth | Sets every quick budget |
| P6 | `QuickArmy.TargetOf` | A family's army lacking a class shares that class's slots among its other classes (Checkers: a host of Men) | Shapes Palamedes, Desdemona and the like |
| P7 | `BoardPrices` | A piece's price never below 1; prices measured from each seat's view; the Western Pawn's start-row powers worth 3 | Prices of weak pieces |
| P8 | pieces.md, Custody | Only the custodian's own move captures; a piece moving between two enemies is safe | A Tafl rule many play otherwise |
| P9 | pieces.md, multiple captures | What taking several at once is worth | Scores of Checkers and Tafl chains |
| P10 | pieces.md, the fled Tafl King | While it is away its army plays on as one whose leader was taken | How fleeing plays |
| P11 | rules.md §4 | Pawn swap: needs a Pawn's die; "facing" means each on the other's next square; whoever may move either Pawn may swap; nothing scores | The whole swap rule's details |
| P12 | rules.md §6.3 | Every die the occupier leaves unused goes back to the occupied player | Shared turns |
| P13 | rules.md §10, retirement | A retired player may rejoin only while they have a piece besides their Raja | Retiring as a shield |
| P14 | rules.md §10, giving up | A team ahead ends the game by giving up only at least 10 ahead on points and on the board | You said "try 10"; the both-margins reading and "being level is not enough" are mine |
| P15 | rules.md §10, proposals | Who may propose an end, and when; the game pauses while the others answer, clockwise | Ending games |
| P16 | specification §6 | Hiring lowers the hirer's score, as any spending does; open only to (see §6) | Mercenaries' cost |
| P17 | specification | A lift belongs to a restriction, not to the kind first given it | What a promoted piece keeps |
| P18 | specification §3 | A palace may move only where it still holds the throne and every palace piece | Drafting |
| P19 | specification | Mercenary prices rounded up | Hiring costs |
| P20 | `GrandLegionsRules` | Grand Legions played by Chaturaji 2027's rules: each leader a Raja, the clear lead × 10 | The base of the whole game |

None is open now (2026-11-27; Q-R, recruiting in play, answered the same day, is in the [backlog](backlog.md)), but
the proposals above.
The player out of a game when their leaders are gone is being thought over by the designer, and waits in the
[backlog](backlog.md).
