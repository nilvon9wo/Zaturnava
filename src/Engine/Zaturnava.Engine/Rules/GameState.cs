using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// Everything a ruleset needs to decide what may happen next. <see cref="DecidingSeat"/> is the seat
/// whose decision the game is waiting for; null once the game is over. While the game waits on
/// <see cref="AwaitedDice"/>, chance decides before that seat does.
/// </summary>
public sealed record GameState(
    Position Position,
    ImmutableList<Seat> Seats,
    TeamGraph Teams,
    SeatId? DecidingSeat,
    int TurnNumber,
    ImmutableDictionary<SeatId, int> Scores,
    ImmutableList<Prisoner> Prisoners
)
{
    /// <summary>
    /// Each seat's throne in this game, if it was dealt its own (<see cref="IRulesApplying.Deal"/>); null, if the
    /// thrones are where the rules have them (<see cref="IRulesApplying.Thrones"/>).
    /// </summary>
    public ImmutableDictionary<SeatId, CellId>? Thrones { get; init; }

    /// <summary>
    /// Each seat's palace in this game, its cells, if the game has palaces (<see cref="IRulesApplying.Deal"/>); null,
    /// if it has none. While the armies are drafted, a seat that has not yet placed its palace has none here.
    /// </summary>
    public ImmutableDictionary<SeatId, ImmutableList<CellId>>? Palaces { get; init; }

    /// <summary>
    /// Whether the armies the program deals this game, or fills at random, may hold the pieces a palace holds, in their
    /// palace (<see cref="IRulesApplying.Deal"/>): never, in a game without palaces, nor one dealt before they could.
    /// </summary>
    public bool HasPalacePieces { get; init; }

    /// <summary>
    /// Whether a kingdom too small for a palace has one standing partly outside it, along the board's edge
    /// (<see cref="IRulesApplying.Deal"/>): never, in a game without palaces, nor one dealt before it could.
    /// </summary>
    public bool HasEdgePalaces { get; init; }

    /// <summary>
    /// The edition of the ruleset's pieces the armies the program deals this game, fills at random, or offers in a
    /// draft, are drawn from (<see cref="IRulesApplying.Deal"/>), and of the rules it is played by, a ruleset's later
    /// rules applying from the edition that brought them: 0, in a game dealt before there was another, so that it
    /// replays as it was dealt and played.
    /// </summary>
    public int Edition { get; init; }

    /// <summary>
    /// Where every piece started in this game, if it was dealt its own armies; null, if as the rules start them.
    /// </summary>
    public Position? Start { get; init; }

    /// <summary>
    /// The terrain this game was dealt (<see cref="IRulesApplying.Deal"/>): its rivers, say, numbered as its ruleset
    /// numbers them; 0, the ruleset's own.
    /// </summary>
    public int Terrain { get; init; }

    /// <summary>
    /// The armies being drafted, if the game began so (<see cref="IRulesApplying.Deal"/>) and play has not yet begun;
    /// null otherwise.
    /// </summary>
    public ArmyDraft? Draft { get; init; }

    /// <summary>
    /// What each seat kept of its draft's budget unspent, which began its score (Stated, 2026-11-12): part of its score
    /// in the game, but not earned in play, so counted in no one's points or experience. None, for a game not drafted.
    /// </summary>
    public ImmutableDictionary<SeatId, int> Unspent { get; init; } = [];

    /// <summary>What <paramref name="seat"/> scored in play: its score, less what it kept of its draft.</summary>
    public int EarnedBy(SeatId seat) => this.Scores.GetValueOrDefault(seat) - this.Unspent.GetValueOrDefault(seat);

    /// <summary>The dice the game waits on, rolled for <see cref="DecidingSeat"/>; null if none.</summary>
    public DiceRequest? AwaitedDice { get; init; }

    /// <summary>The dice rolled this turn and not used yet, in the order they were rolled.</summary>
    public ImmutableList<int> Dice { get; init; } = [];

    /// <summary>
    /// The seats still rolling to see who goes first, in the order they roll, with what they have
    /// rolled so far in <see cref="Dice"/>; empty once someone has won.
    /// </summary>
    public ImmutableList<SeatId> RollOff { get; init; } = [];

    /// <summary>The seat whose turn <see cref="DecidingSeat"/> is playing for it; null if its own.</summary>
    public SeatId? ActingFor { get; init; }

    /// <summary>How many turns in a row have been passed without moving, whether the dice were rolled or not.</summary>
    public int IdlePasses { get; init; }

    /// <summary>How many turns have passed since a piece was last taken.</summary>
    public int TurnsSinceCapture { get; init; }

    /// <summary>
    /// Each position a turn has ended in since a piece was last taken, with the prisoners and the seat to play
    /// then, kept as a key: equal positions have equal keys.
    /// </summary>
    public ImmutableHashSet<long> Seen { get; init; } = [];

    /// <summary>How many turns in a row, each moving, have ended in a position seen before.</summary>
    public int RepeatedTurns { get; init; }

    /// <summary>
    /// Whether the occupier of the throne of the seat whose turn it is has left that seat the dice it did not
    /// use, so that it moves for that seat no more this turn.
    /// </summary>
    public bool IsHandedBack { get; init; }

    /// <summary>The ransom waiting on its receiver, who decides meanwhile; null if none is.</summary>
    public RansomOffer? Offer { get; init; }

    /// <summary>
    /// Whether the turn is over and its seat, deciding, may still offer ransoms before it passes; the
    /// turn passes with <see cref="IdlePasses"/> as they stand.
    /// </summary>
    public bool IsRansomWindow { get; init; }

    /// <summary>
    /// The turn on which each prisoner taken by its own team was taken: it may go home only once every seat has
    /// had a turn since.
    /// </summary>
    public ImmutableDictionary<PieceId, int> TakenByOwnTeam { get; init; } = [];

    /// <summary>The ransoms offered this turn, which may not be offered again until it passes.</summary>
    public ImmutableList<RansomOffer> OffersMade { get; init; } = [];

    /// <summary>
    /// The ransoms declined so far this game, each with when it was, and the board then, so that a player (a bot,
    /// say) need not ask the same again until something has changed; forgotten between two seats once the one
    /// that declined makes the other an offer.
    /// </summary>
    public ImmutableList<DeclinedOffer> OffersDeclined { get; init; } = [];

    /// <summary>
    /// The cell of a Pawn just come to the far edge, which the deciding seat may promote at once, before
    /// the turn goes on, or leave for later; null if none.
    /// </summary>
    public CellId? PromotionDue { get; init; }

    /// <summary>
    /// The moves straight ahead of two cells or more that may still be taken <i>en passant</i>: each by a seat until
    /// its own turn ends (<see cref="Passage.Closed"/>), or the piece moves on.
    /// </summary>
    public ImmutableList<Passage> Passages { get; init; } = [];

    /// <summary>
    /// Prisoners going home that wait on where the first is to go, chosen by its owner, the deciding
    /// seat; null if none.
    /// </summary>
    public ReturnsDue? Returning { get; init; }

    /// <summary>
    /// The pieces that fled the board of their own accord, as the King of Tafl escapes at a corner
    /// (<see cref="Actions.FleeAction"/>): no one holds them, nor may take them, until they come back as a prisoner
    /// escapes.
    /// </summary>
    public ImmutableList<Piece> Fled { get; init; } = [];

    /// <summary>
    /// The mercenaries hired (<see cref="Actions.HireAction"/>), their hirer's own now, waiting to be placed on the
    /// board (<see cref="Actions.DeployAction"/>): no one may take them meanwhile.
    /// </summary>
    public ImmutableList<Piece> Hired { get; init; } = [];

    /// <summary>
    /// What each mercenary cost last, where that is not its starting kind's value: once hired, or promoted into a
    /// dearer piece. Its next hire costs 110% of it.
    /// </summary>
    public ImmutableDictionary<PieceId, int> MercenaryCosts { get; init; } = [];

    /// <summary>A proposal to end the game, waiting on the deciding seat's answer; null if none is.</summary>
    public EndProposal? Proposal { get; init; }

    /// <summary>The seats that have given up their turns, having proposed ending the game.</summary>
    public ImmutableHashSet<SeatId> Resigned { get; init; } = [];

    /// <summary>The seats that ended their part in the game, left with only their leader.</summary>
    public ImmutableHashSet<SeatId> Retired { get; init; } = [];

    /// <summary>
    /// Whether a seat has scored for commanding every piece on the board since every seat last had a piece on
    /// it, so that no one may score for it again until every seat does.
    /// </summary>
    public bool IsEveryPieceScored { get; init; }

    /// <summary>
    /// The points each seat has farmed by stepping its leader on and off a throne, which it loses should the
    /// leader be taken before it does anything else; a seat that farmed none is not here.
    /// </summary>
    public ImmutableDictionary<SeatId, int> Farmed { get; init; } = [];

    /// <summary>The seats whose leader last stepped off a throne idly, so that stepping back on farms.</summary>
    public ImmutableHashSet<SeatId> SteppedOff { get; init; } = [];

    /// <summary>Who is away from the game, and who would stop waiting for them.</summary>
    public Absence Absence { get; init; } = Absence.None;

    /// <summary>How the game ended; null while it goes on.</summary>
    public GameOutcome? Outcome { get; init; }

    public Seat SeatOf(SeatId id) => this.Seats.Single(seat => seat.Id == id);
}
