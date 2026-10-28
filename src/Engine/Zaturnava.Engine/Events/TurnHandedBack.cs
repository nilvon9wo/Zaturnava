using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Occupier"/>, moving for <see cref="Seat"/>, left the dice it had not used to <see cref="Seat"/>.
/// </summary>
public sealed record TurnHandedBack(SeatId Occupier, SeatId Seat) : GameEvent;
