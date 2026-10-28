using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/>, left with only its Raja, ended its part in the game.</summary>
public sealed record RajaRetired(SeatId Seat) : GameEvent;
