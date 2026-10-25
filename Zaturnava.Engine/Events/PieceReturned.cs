using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>A prisoner went home, to <see cref="Cell"/>, for its own army.</summary>
public sealed record PieceReturned(Piece Piece, CellId Cell) : GameEvent;
