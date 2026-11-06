using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Occupier"/> holds <see cref="Seat"/>'s throne, and moves for it now.</summary>
public sealed record TurnShared(SeatId Seat, SeatId Occupier) : GameEvent
{
    /// <summary>Whether <see cref="Seat"/> passed on its own move, leaving its dice to the occupier.</summary>
    public bool HasPassed { get; init; }
}
