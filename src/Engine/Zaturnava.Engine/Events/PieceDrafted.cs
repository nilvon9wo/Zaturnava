using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// While the armies are drafted, <see cref="Seat"/> bought a piece of <see cref="Kind"/> for <see cref="Price"/>, and
/// placed it on <see cref="Cell"/>.
/// </summary>
public sealed record PieceDrafted(SeatId Seat, PieceKindId Kind, CellId Cell, int Price) : GameEvent;
