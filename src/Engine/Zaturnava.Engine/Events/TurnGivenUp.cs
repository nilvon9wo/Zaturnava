using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/>, having given up its turns, passed this one.</summary>
public sealed record TurnGivenUp(SeatId Seat) : GameEvent;
