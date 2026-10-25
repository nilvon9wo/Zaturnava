using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="PromotedBy"/> promoted <see cref="Pawn"/> on <see cref="Cell"/> into the captured
/// <see cref="Into"/>, which stands there now, for its own army.
/// </summary>
public sealed record PiecePromoted(Piece Pawn, Piece Into, CellId Cell, SeatId PromotedBy) : GameEvent;
