using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Seat"/> bought off the <see cref="Restriction"/> of its piece of <see cref="Kind"/> on
/// <see cref="Cell"/>, for <see cref="Price"/>.
/// </summary>
public sealed record RestrictionLifted(SeatId Seat, PieceKindId Kind, CellId Cell, LiftKind Restriction, int Price)
    : GameEvent;
