using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>
/// What anyone - human, bot or AI - is given when it is their seat's turn to decide. With
/// <see cref="Rules"/>, a bot can look ahead: what each legal action would lead to.
/// </summary>
public sealed record DecisionRequest(
    IRulesApplying Rules,
    GameState State,
    SeatId Seat,
    ImmutableList<GameAction> LegalActions
)
{
    /// <summary>The seats people play, so that a bot may defer to them; empty if none is, or it is not known.</summary>
    public ImmutableHashSet<SeatId> People { get; init; } = [];
}
