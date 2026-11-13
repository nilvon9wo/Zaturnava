using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>While the armies are drafted, <see cref="Seat"/> set its throne on <see cref="Cell"/>.</summary>
public sealed record ThroneSet(SeatId Seat, CellId Cell) : GameEvent;
