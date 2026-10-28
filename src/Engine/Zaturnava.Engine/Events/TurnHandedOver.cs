using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> let its ally <see cref="Regent"/> take the rest of its turn.</summary>
public sealed record TurnHandedOver(SeatId Seat, SeatId Regent) : GameEvent;
