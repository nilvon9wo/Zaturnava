using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Seat"/> was away, so passed, as the others voted: <see cref="Missed"/> turns in a row now,
/// of the <see cref="Limit"/> after which it gives up its turns.
/// </summary>
public sealed record TurnPassedAway(SeatId Seat, int Missed, int Limit) : GameEvent;
