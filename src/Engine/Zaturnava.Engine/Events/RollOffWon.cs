using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> rolled highest, <see cref="Value"/>, and goes first.</summary>
public sealed record RollOffWon(SeatId Seat, int Value) : GameEvent;
