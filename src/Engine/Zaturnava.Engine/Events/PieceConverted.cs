using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Piece"/>, as it was, on <see cref="Cell"/>, became <see cref="ConvertedTo"/>'s piece, where it stands:
/// converted, as by the Evangelist.
/// </summary>
public sealed record PieceConverted(Piece Piece, SeatId ConvertedTo, CellId Cell) : GameEvent;
