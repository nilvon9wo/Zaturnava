using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// The hired mercenary <see cref="Piece"/>, its hirer's own, was placed on the board at <see cref="Cell"/>.
/// </summary>
public sealed record PieceDeployed(Piece Piece, CellId Cell) : GameEvent;
