using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// While the armies are drafted all at once, <see cref="Seat"/> took back the piece of <see cref="Kind"/> it had placed
/// on <see cref="Cell"/>, and was refunded its <see cref="Price"/>.
/// </summary>
public sealed record PieceUndrafted(SeatId Seat, PieceKindId Kind, CellId Cell, int Price) : GameEvent;
