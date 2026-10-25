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

    /// <summary>The ransom waiting on its receiver, who decides meanwhile; null if none is.</summary>
    public RansomOffer? Offer { get; init; }

    /// <summary>
    /// Whether the turn is over and its seat, deciding, may still offer ransoms before it passes; the
    /// turn passes with <see cref="IdlePasses"/> as they stand.
    /// </summary>
    public bool IsRansomWindow { get; init; }

    /// <summary>
    /// The cell of a Pawn just come to the far edge, which the deciding seat may promote at once, before
    /// the turn goes on, or leave for later; null if none.
    /// </summary>
    public CellId? PromotionDue { get; init; }

    /// <summary>How the game ended; null while it goes on.</summary>
    public GameOutcome? Outcome { get; init; }

    public Seat SeatOf(SeatId id) => this.Seats.Single(seat => seat.Id == id);
}
