using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>While the armies are drafted, <see cref="Seat"/> placed its palace about <see cref="Centre"/>.</summary>
public sealed record PalacePlaced(SeatId Seat, CellId Centre) : GameEvent;
