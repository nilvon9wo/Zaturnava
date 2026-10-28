using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Moves;

public sealed record Move(Piece Mover, CellId From, CellId To, Piece? Captured);
