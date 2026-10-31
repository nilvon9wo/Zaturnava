using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
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

    /// <summary>How many turns in a row have been passed without rolling or moving.</summary>
    public int IdlePasses { get; init; }

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
    /// The ransoms declined so far this game, kept so that a player (a bot, say) need not ask the same again.
    /// </summary>
    public ImmutableList<RansomOffer> OffersDeclined { get; init; } = [];

    /// <summary>
    /// The cell of a Pawn just come to the far edge, which the deciding seat may promote at once, before
    /// the turn goes on, or leave for later; null if none.
    /// </summary>
    public CellId? PromotionDue { get; init; }

    /// <summary>
    /// Prisoners going home that wait on where the first is to go, chosen by its owner, the deciding
    /// seat; null if none.
    /// </summary>
    public ReturnsDue? Returning { get; init; }

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
