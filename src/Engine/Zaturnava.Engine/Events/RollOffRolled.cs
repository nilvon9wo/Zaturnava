using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> rolled <see cref="Value"/> to see who goes first.</summary>
public sealed record RollOffRolled(SeatId Seat, int Value) : GameEvent;
