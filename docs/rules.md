# Zaturnava — Rules Specification (v0.1 draft)

Canonical, unambiguous ruleset for the base configuration: 4 players, teams of 2
seated opposite each other, standard 8×8 board, traditional piece set. This is
the reference the engine is built against. Anything marked **[toggle]** is a
named configuration switch — see [§12 Configuration Toggles](#12-configuration-toggles)
for the summary table. Broader variant support (different team topologies,
board shapes/sizes, army counts, etc.) is an architecture concern, covered in
[architecture.md](architecture.md), not a rules concern — this document
describes one concrete, fully-specified configuration.

Source material: the original [rules write-up](origins/chaturaji.md) and
[score sheet](origins/score-sheet.md), clarified and corrected through design
discussion on 2026-09-11 and 2026-09-12. Where this document and the originals
disagree, this document is authoritative — the discrepancies were deliberate corrections,
not accidents (see inline notes marked *(clarified)*).

## Implementation status

What the `classic` ruleset (`Zaturnava.Variants.Classic`) enforces so far.
Everything else here is specified but not yet played by the engine.

| Section | Status |
|---|---|
| §2 Board & setup | Implemented: the 8×8 board, the pinwheel setup, teams of opposite seats. Thrones not yet. |
| §3 Pieces & movement | Implemented, captures included. |
| §4 Turns & dice | Partly: a turn is one deliberate move or a pass, clockwise from seat 0. No dice yet. |
| §5 Captures & scoring | Partly: captures score by piece. No bonuses yet. |
| §6–§9 Throne control, doubles, promotion and ransom, boat triumph | Not yet. |
| §10 Game end | Not yet: a game does not end. |
| §11 Setup roll-off | Not yet. |
| §12 Toggles | None yet; the defaults are fixed. |

## 1. Overview

Zaturnava is a 4-player chaturaji variant. Unlike chess, the objective is not
to checkmate but to accumulate points by capturing pieces; a raja (king) being
captured does not end the game, and captured rajas can be returned to play.
Played in teams of 2, with teammates seated opposite each other on the board.

## 2. Board & Setup

- 8×8 square board, traditionally unchequered (no alternating colors required).
- Four armies, one per corner, arranged with 4-fold rotational symmetry
  (a "pinwheel"). Each army occupies a 2×4 rectangle at its corner.
- Within that rectangle, the back rank runs along the *outer* board edge, in
  this order from the corner inward toward the board center:
  **Boat, Horse, Elephant, Raja** — so the Raja always starts on the square
  closest to the board's center, and the Boat on the outermost corner square.
  The 4 Pawns fill the adjacent rank/file, directly in front of their
  corresponding back-rank piece.
- Each player's starting Raja square is that player's **throne**. In this
  base configuration the throne is simply the fixed starting square for the
  whole game. *(Open question, not yet decided: whether a throne should track
  a relocated Raja starting square in configurations that vary where the Raja
  starts — this only matters once such a variance exists; see
  [architecture.md §10](architecture.md#10-open-questions--deferred).)*
- Each army: 1 Raja, 1 Horse, 1 Elephant, 1 Boat, 4 Pawns (8 pieces, 32 total
  on the board at start).
- Traditional colors: black, yellow, green, red (no gameplay significance).

## 3. Pieces & Movement

| Piece | Movement |
|---|---|
| **Raja** (king) | One square in any direction. |
| **Elephant** | Any number of squares orthogonally (like a rook); blocked by intervening pieces. |
| **Horse** (knight) | Standard knight L-move; jumps over intervening pieces. |
| **Boat** | Exactly two squares diagonally, in any of the 4 diagonal directions; jumps over whatever occupies the intervening square (friend or foe) without capturing it. Lands on and, if occupied by an enemy, captures whatever is on the destination square. |
| **Pawn** | One square in its army's "forward" direction (see below); captures one square diagonally forward. No initial double-step, no en passant. |

**Forward direction** is not a single global direction — each army's pawns
move straight toward the board's center, i.e. forward is rotated 90° between
each adjacent army. (Confirmed from the original document's move diagram.)

### Captures

All pieces except the Pawn capture the same way they move, by landing on an
occupied square. The Pawn captures only diagonally forward, never straight.

## 4. Turns & Dice

Turn order is clockwise around the board, starting from whichever player wins
the opening roll-off (each player rolls once; highest wins; re-roll ties).
The opening move is special only in that the first player doesn't get the
usual deliberate-move-vs-dice choice: they roll a single die (not two) and
may move the indicated piece category, with no second move. *(Clarified)*
Moving it is still not mandatory — nothing ever is, see below — and if the
indicated category has no legal move at all, no move happens. In particular,
rolling the Elephant category (3 or 4 on the default profile) on the opening
move is always a no-op: the Elephant starts fully blocked by its own Pawn and
has no legal move yet.

On every subsequent turn, the player either:

- **Moves deliberately**: picks any one legal piece to move, no dice involved; or
- **Rolls two dice**: each die indicates a piece *category* to move (see dice
  table below); the player may move a piece from each category rolled (one
  move per die), in either order, or decline either or both moves. If both
  dice show the same value ("doubles"), see [§7 Doubles & Escape](#7-doubles--escape).
- If a rolled category has no legal move available, that die's move is
  forfeit (forced skip) for that category only — this is *not* a full-turn
  skip unless both dice come up unusable.

Both the "which piece" (direct move) and "whether to take an offered move" are
always the player's choice — movement is simply never mandatory, full stop,
including the opening move.

### Dice profile **[toggle: `dice.profile`]**

The original game used two 4-sided dice; in practice most players don't own
d4s, so the *default* profile below uses two ordinary d6 with a weighted
mapping. A pure d4 profile is also supported as an alternative (historically
"correct," but its probability distribution has never actually been
playtested — this is exactly the kind of thing the engine should let you swap
without touching code).

**Default profile — `2d6-weighted`:**

| Die value | Piece category |
|---|---|
| 1 | Boat |
| 2 | Horse |
| 3 or 4 | Elephant |
| 5 or 6 | Pawn or Raja (player's choice which) |

**Alternative profile — `2d4-uniform`:**

| Die value | Piece category |
|---|---|
| 1 | Boat |
| 2 | Horse |
| 3 | Elephant |
| 4 | Pawn or Raja (player's choice which) |

## 5. Captures & Scoring

Points are awarded for the *act* of capturing, not for permanently holding
the piece — a returned/ransomed piece keeps the original captor's points, and
if it's captured *again* later (by anyone), that capture scores again too.
Points are never subtracted or returned when a piece is given back.

| Captured piece | Points |
|---|---|
| Pawn | 1 |
| Boat | 2 |
| Horse | 3 |
| Elephant | 4 |
| Raja | 5 |

### Bonus points

- **+1** each time a player's Raja *enters* an opponent's throne square
  (leaving and re-entering scores again, but may be poor strategy).
- **+2** if a player captures an opponent's Raja *on that opponent's own
  throne square* (separate from, and in addition to, any entry bonus scored
  when moving onto that square).
- **"Creating the empire"**: a player who is currently commanding allied
  pieces (i.e. has [Controlled Armies](architecture.md#24-controlled-armies--the-unifying-primitive) that
  include an ally's army — see §6) at a moment when **both enemy rajas are
  simultaneously off the board** (captured and not yet returned) triggers:
  - **+1** for creating the empire,
  - **+2** additionally if this player personally captured the last
    surviving enemy Raja to cause this state,
  - **+4** instead of the +2 if that last enemy Raja was captured on its own
    throne square.
  - *(Clarified)* This bonus is **edge-triggered and repeatable, never
    reversed**: it fires again every time the "both enemy rajas currently
    off the board" state is freshly re-entered (i.e. after at least one of
    the enemy rajas has returned to play and been captured again), and prior
    awards are never clawed back even if a captured raja is later ransomed
    back.

## 6. Throne Control

Every seat has one throne (its player's starting Raja square). Throne
occupation by a *foreign* Raja (ally's or enemy's) changes both the occupier's
and the occupied player's turns. There are three distinct mechanics here —
treat them as separate rules, not one generic "occupation" rule:

### 6.1 Expanded pool (on the owning player's own turn)

A player's legal piece pool for their own turn can be expanded beyond their
own army by **either** of two independent triggers — they stack if both
apply:

(a) **Throne occupation**: whenever a player's Raja currently sits on **any**
other seat's throne (ally's or enemy's), the throne-owner's army is added to
their pool for that turn.

(b) **Raja-less ally regency**: *(clarified)* whenever a player's ally
currently has **no Raja on the board at all** — the same condition as §6.2
case (b) below — that ally's army is added to the player's pool for that
turn too, **even if the player isn't occupying that ally's throne**. This is
distinct from §6.2: §6.2 is about the ally taking over the *Raja-less
player's own turn*; this is about the Raja-having player's *own separate
turn* automatically reaching into the Raja-less ally's army, with no
delegation, consent step, or throne-sitting required — it's a standing
consequence of the ally having no Raja to call their own pieces' shots for
them.

Either way, turn structure is unchanged (still one direct move, or dice as
normal) — only the pool of pieces eligible to be moved is expanded.

*This "expanded pool" is a property of the specific player whose Raja is
sitting on the foreign throne, or who is regenting a Raja-less ally — it
belongs to them personally, not to whichever player happens to be making a
given move.* This matters for §6.2 and §6.3 below.

### 6.2 Voluntary regency (on the occupied player's own turn, ally case)

If, on a player's own turn, **either** (a) their ally's Raja currently
occupies their throne, **or** (b) they currently have no Raja on the board at
all (regardless of who if anyone occupies their throne) — then their ally
*may* take that turn on their behalf, using only the occupied player's own
native army (**not** the ally's expanded pool from §6.1 — the expanded-pool
privilege does not transfer to a delegated decision-maker).

*(Clarified)* This regency is only possible while the ally themself currently
has their own Raja on the board — an ally with no Raja of their own has
nothing to be regent *with* and cannot act as regent for anyone (they may
still be on the *receiving* end of case (b) themself, waiting for their own
ally to act for them).

Case (b) above also triggers §6.1(b): the Raja-holding ally gets that
Raja-less ally's army folded into their own expanded pool on their own
separate turn, automatically and without a consent step — a standing effect,
not a per-turn delegation like this section's handoff.

This handoff requires the occupied player's consent each time, and is
**granular**: the occupied player can delegate the whole turn, or only part
of it (e.g. let the ally decide direct-move-vs-dice, or roll it themself and
hand off only the piece selection, or make the first of two dice-moves
themself and hand off only the second).

### 6.3 Forced shared turn (on the occupied player's own turn, enemy case)

If, on a player's own turn, an **enemy's** Raja currently occupies their
throne:

1. The occupied player **must roll dice** — no direct-move option.
2. The occupied player takes the first move themself if they want it.
   *(Clarified)* If the occupied player themself currently qualifies for an
   [expanded pool](#61-expanded-pool-on-the-owning-players-own-turn) (§6.1) —
   because they're occupying some *other* throne (§6.1a), or because their
   own ally currently has no Raja on the board (§6.1b) — they take this first
   move from that expanded pool rather than being restricted to just their
   native army; that privilege is theirs regardless of whose turn triggered
   it. Movement is still optional; declining means the occupier gets both
   dice-moves instead of one.
   - *(Clarified)* If this first move **liberates the occupied player's own
     throne** — i.e. one of the occupied player's own pieces (native army, or
     expanded pool per the case above) captures the enemy Raja sitting on
     their throne — the occupied player gets the **second** move back too:
     the throne is no longer enemy-occupied, so the forced-shared-turn
     condition that gave the occupier a move no longer holds for the rest of
     this turn.
3. Otherwise, the occupying enemy then takes the second move, using only the
   occupied player's native army (never their own separate army — that's a
   different turn, covered by §6.1 when it comes around).

Aside from the expanded-pool exception in step 2 above, neither side moves
their own separate army during this shared turn — it is otherwise entirely
about the occupied player's army for that turn.

## 7. Doubles & Escape

When a player rolls dice and both dice show the same value ("doubles"), two
things become available, and the player freely chooses between them (not
forced into either):

- **Double move**: if the indicated category has a piece currently on the
  board within the player's [controlled armies](architecture.md#24-controlled-armies--the-unifying-primitive)
  for this turn, the player *may* (never must — see below) make two moves
  instead of one from that category. *(Clarified)* Choosing to use the double
  move does not obligate moving the *same* piece twice: if more than one
  piece of the indicated category is available across the player's controlled
  armies (e.g. two Pawns, or a Pawn plus an ally's Pawn via an expanded pool),
  the two moves may be split between different pieces of that category,
  player's choice.
- **Escape**: if there is a *valid captured piece* of the indicated category
  currently held prisoner, belonging to **any army currently within the
  player's controlled-armies scope for this decision** (their own army; an
  ally's army they currently have regency over per §6.2; or an army whose
  throne they're occupying per §6.1) — that piece returns unconditionally to
  its own starting square, **no ransom consent required**:
  - If an enemy piece occupies that starting square, the escaping piece still
    lands there and captures it (scored normally).
  - If an ally's piece occupies it, the escaping piece instead goes to the
    closest free square (player's choice among equidistant options) — same
    placement rule as [Raja return](#raja-return) below.

If multiple valid captured pieces qualify (e.g. both the player's own captured
Raja and an ally's captured Raja qualify under a "5 or 6" roll), the player
chooses which one escapes.

*(Clarified)* This is deliberately open to tactical abuse in both directions:
a player may escape an **ally's** captured piece (because they currently hold
regency over that ally) to strengthen the team, or may escape an **enemy's**
captured piece (because they're occupying that enemy's throne) specifically
so an ally can re-capture it on the ally's very next turn for extra points,
before the enemy gets a chance to move it to safety.

## 8. Promotion & Ransom

*(Clarified)* Promotion and Ransom are two **independent** mechanics that
happen to both involve the shared captured-piece pool — they are not two
flavors of one "release a captured piece" rule:

- **Promotion never requires anyone's consent.** A promoting Pawn simply
  consumes an eligible captured piece from the pool unconditionally.
- **Ransom always requires mutual consent** — both parties must agree to the
  specific trade. This is a fixed rule, not a toggle.

### Promotion

If a Pawn reaches the board's far edge (the edge opposite its own army's
starting edge), it may be promoted — but **only if there is a valid captured
piece for it to become**; a Pawn that reaches the far edge with no eligible
captured piece available simply cannot move again until one becomes
available (via a future capture, by anyone).

- **[toggle: `promotion.pool`]** — Default: **unrestricted**. The Pawn may
  become *any* currently-captured piece, belonging to any player's captured
  stock, not just a piece that started on the same file/column. *(Clarified:
  the original file/column restriction is preserved as an optional toggle,
  but is off by default — in practice a Pawn rarely reached the far edge at
  all, and when it did, restricting the reward to whatever specific piece
  started on that file was often either unavailable or impractically weak,
  e.g. forced into a Boat.)*
- If the captured piece the Pawn is promoted into belonged to a **different**
  player than the promoting Pawn's owner, the resulting piece belongs to
  *that other player*, not to the promoting player. (Promotion consumes a
  captured piece from the shared captured-piece pool; it does not
  manufacture a new piece for the promoter's own army unless the piece being
  consumed was already theirs.)
- Promotion is not mandatory the instant a Pawn qualifies — it may be
  deferred indefinitely. When deferred, the choice of *when* and *into what*
  belongs to whichever player currently controls that Pawn's army on a given
  turn (the owner normally, or an ally/enemy currently controlling that army
  under §6). No one's consent is needed to consume the captured piece it's
  promoted into, regardless of who currently holds that piece prisoner.

### Ransom

*(Clarified)* Ransom is a general trade mechanic for captured pieces — it is
**not** specific to the Raja, and it is a separate concept from Promotion
(§8 above already covers that distinction).

- On a player's turn — either before or after their move for that turn — if
  they currently hold prisoner one or more pieces belonging to another
  player, they may offer that player an exchange of captured pieces.
- The exchange is negotiated freely: the number and point-value of pieces
  offered on each side does **not** need to match.
- The trade only happens if **both players agree** to that specific offer —
  ransom always requires mutual consent, with no toggle to make it automatic
  or one-sided (contrast Promotion, which never needs consent from anyone).
- Each piece returned by a ransom goes back as close as possible to its own
  home/starting square. If that exact square is occupied, the *receiving*
  player chooses among the equidistant free squares — landing on and
  capturing whatever occupies the home square is **not** an option for a
  returning piece.
- Releasing a piece via ransom never returns the points already scored for
  capturing it, and if the released piece is captured again by anyone, that
  capture scores again.
- *(Open item)* The exact turn-timing protocol (how an offer is made and
  responded to without stalling play while the other player decides) needs
  further refinement — see [§13](#13-open-items--deferred).

### Raja Return

A captured Raja can return to play **three** ways:

1. **Escape**: on a doubles roll, an eligible Raja returns unconditionally,
   no consent from anyone required — see [§7 Doubles & Escape](#7-doubles--escape).
2. **Ransom exchange**: if a player holds an opponent's captured Raja and
   that opponent holds a piece belonging to the first player (Raja or
   otherwise), either side may propose a ransom trade per the general Ransom
   mechanic above — requires mutual consent, need not be Raja-for-Raja in
   value or kind. Each returned Raja goes back to its own starting square,
   or — if occupied — the closest free square, chosen by the *receiving*
   player when multiple squares are equidistant.
3. **Promotion**: a Pawn reaching the far edge may be promoted into a
   captured Raja, per §8 above — no consent required from whoever currently
   holds that Raja prisoner.

## 9. Boat Triumph

If all 4 Boats — from all 4 armies — occupy the four cells of the same 2×2
square simultaneously, the player who made the move that completed this
formation captures **every other army's Boat** in that square. (Their own
Boat, and an ally's Boat if present in the square, are unaffected.)

After a Boat Triumph, until the captured Boat(s) are themselves captured,
both the triumphing player and their ally may move each other's Boat.

## 10. Game End

- **Elimination**: if a player loses every piece, their ally continues playing
  alone for the rest of the game.
- **Voluntary Raja retirement**: a player reduced to only their Raja may
  choose to end their own participation; no points are ever awarded for
  capturing that Raja from then on.
- **Honourable peace**: if only Rajas remain on the board (across all
  players), the game is a draw.
- **Team elimination win**: if only one team still has any pieces on the
  board at all, that team wins outright and accumulated points are
  disregarded.
- **Ordinary end — by agreement**: *(clarified)* otherwise, there is no
  single hard-coded end condition. The game is expected to end when the
  players agree it should — either agreeing one team has won (e.g. the point
  gap is large enough, and the trailing team has no realistic path back given
  remaining pieces) or agreeing to a draw. This is comparable to how Go games
  end by mutual agreement rather than forced checkmate.
  - **[toggle group: `gameEnd.safetyNet.*`]** — because unattended games
    (bot vs. bot, or an abandoned async game) can't rely on human judgment to
    "agree," the engine should support optional supplementary auto-end
    conditions layered on top of the agreement mechanic — e.g. a move-count
    cap, or "every remaining player passed on their last turn." These are
    off by default for human games and are an architecture/engine concern
    (see architecture.md) more than a core rules concern — exact parameters
    are deliberately left open for now.

## 11. Setup Roll-off

Each player rolls one die (matching whatever dice profile is configured) to
determine seating/turn order; highest goes first, re-rolling to break ties.

## 12. Configuration Toggles

Summary of every named toggle introduced above. Defaults reflect the base
configuration this document describes.

| Toggle | Default | Description |
|---|---|---|
| `dice.profile` | `2d6-weighted` | Dice count/sides and value→piece-category mapping. Alternative: `2d4-uniform`. |
| `promotion.pool` | `unrestricted` | Whether a promoting Pawn may become any captured piece, or only one that started on the same file/column. |
| `gameEnd.safetyNet.*` | off | Optional auto-end conditions (move cap, universal-pass, etc.) layered on top of end-by-agreement, mainly for unattended/bot games. |
| `scoring.points.pawn` | `1` | Points awarded for capturing a Pawn. |
| `scoring.points.boat` | `2` | Points awarded for capturing a Boat. |
| `scoring.points.horse` | `3` | Points awarded for capturing a Horse. |
| `scoring.points.elephant` | `4` | Points awarded for capturing an Elephant. |
| `scoring.points.raja` | `5` | Points awarded for capturing a Raja. |
| `scoring.bonuses.throneEntry` | `1` | Bonus for a Raja entering an opponent's throne square (§5). |
| `scoring.bonuses.rajaCapturedOnThrone` | `2` | Additional bonus for capturing an opponent's Raja on that opponent's own throne square (§5). |
| `scoring.bonuses.empireCreated` | `1` | "Creating the empire" base bonus (§5). |
| `scoring.bonuses.empireCreatedByCapture` | `2` | Additional empire bonus when the player personally captured the triggering Raja (§5). |
| `scoring.bonuses.empireCreatedByThroneCapture` | `4` | Replaces the previous bonus when that capture happened on the enemy Raja's own throne (§5). |
| `pieceSet.naming` | `traditional` | Display naming only, no movement change: `traditional` (Pawn/Boat/Horse/Elephant/Raja) vs. `modern` (Pawn/Boat/Knight/Rook/King). *(Newly raised — which chess-standard labels map to which traditional piece, and whether "Boat" itself is replaced by something else entirely, is still undecided; see [§13](#13-open-items--deferred).)* |

*(Clarified)* There is deliberately no `ransom.requiresCapturerConsent` (or
similar) toggle: consent behavior for Promotion (never required) and Ransom
(always required, mutual) are both fixed rules described in §8, not
configurable — see that section for why they're kept as two independent,
non-toggleable mechanics.

*(This table will grow as the architecture doc's broader configuration
surface — board topology, team graph, army count, etc. — gets implemented;
those are structural/architectural choices rather than per-game rule toggles,
and are tracked in [architecture.md](architecture.md) instead.)*

## 13. Open Items / Deferred

Nothing rules-level is currently blocking. Noted for later attention:

- Exact default parameters for the `gameEnd.safetyNet.*` toggles (move cap
  size, etc.). Bots now exist; until a game can end (§10), the server only
  bounds how many bot moves one request may play (`Games:MaximumBotSteps`,
  architecture.md §6) — that is a hosting limit, not a rule.
- Whether team membership can ever change *during* a game (a future variant
  idea raised during design discussion) is explicitly out of scope for now —
  team assignment is fixed at game creation.
- Ransom's turn-timing protocol (§8) — how an offer is presented and
  accepted/declined without stalling play while the other player decides —
  needs refinement once there's an actual turn-flow/UI to design it against.
- **Piece naming/set**: *(updated)* traditional-vs-chess-standard naming is
  now the `pieceSet.naming` toggle in §12, rather than a one-time decision —
  players can pick either label set (Pawn/Boat/Horse/Elephant/Raja vs.
  Pawn/Boat/Knight/Rook/King) per game. Still genuinely open: whether to
  replace the Boat specifically with a different piece *identity* (not just a
  chess-standard relabeling), since "jumping boats" reads as an odd fit for a
  two-square diagonal leap — a kangaroo, or some other jumping-diagonal-mover
  flavor, was floated as an alternative. That would need its own naming-set
  entry (or a third `pieceSet.naming` option) once decided. Purely
  cosmetic/identity, per
  [architecture.md §2.2](architecture.md#22-piece-catalog-and-movement-grammar)
  — the movement grammar doesn't change either way — but left open since it
  affects every rules doc and UI label once decided.
- Hosting is not a rules concern; see
  [architecture.md §8](architecture.md#8-tech-stack--hosting).
