using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/>, having given up its turns, took them back.</summary>
public sealed record Rejoined(SeatId Seat) : GameEvent;
