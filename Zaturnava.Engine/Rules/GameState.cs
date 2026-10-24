using System.Collections.Immutable;
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
    ImmutableList<Piece> Prisoners
)
{
    /// <summary>The dice the game waits on, rolled for <see cref="DecidingSeat"/>; null if none.</summary>
    public DiceRequest? AwaitedDice { get; init; }

    /// <summary>The dice rolled this turn and not used yet, in the order they were rolled.</summary>
    public ImmutableList<int> Dice { get; init; } = [];

    public Seat SeatOf(SeatId id) => this.Seats.Single(seat => seat.Id == id);
}
