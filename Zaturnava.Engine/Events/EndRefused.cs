using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> refused to end the game, which goes on.</summary>
public sealed record EndRefused(SeatId Seat) : GameEvent;
