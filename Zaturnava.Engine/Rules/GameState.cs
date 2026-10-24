using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// Everything a ruleset needs to decide what may happen next. <see cref="DecidingSeat"/> is the seat
/// whose decision the game is waiting for; null once the game is over.
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
    public Seat SeatOf(SeatId id) => this.Seats.Single(seat => seat.Id == id);
}
