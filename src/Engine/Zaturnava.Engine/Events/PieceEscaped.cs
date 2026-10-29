using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// A prisoner escaped, back onto the board at <see cref="Cell"/>, freed by <see cref="EscapedBy"/>, taking
/// <see cref="Took"/> if a piece stood there.
/// </summary>
public sealed record PieceEscaped(Piece Piece, CellId Cell, SeatId EscapedBy, Piece? Took = null) : GameEvent;
