using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> agreed to end the game.</summary>
public sealed record EndAgreed(SeatId Seat) : GameEvent;
