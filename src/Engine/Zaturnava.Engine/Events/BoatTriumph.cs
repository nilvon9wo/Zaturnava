using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Triumpher"/> made a Boat triumph, taking the other team's Boats, and <see cref="Seat"/> - the triumpher
/// or its ally - scores <see cref="Points"/> for it on top of the captures themselves.
/// </summary>
public sealed record BoatTriumph(SeatId Triumpher, SeatId Seat, int Points) : GameEvent;
