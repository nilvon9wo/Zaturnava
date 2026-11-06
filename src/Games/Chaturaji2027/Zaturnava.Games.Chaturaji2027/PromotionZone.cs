using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Whether <paramref name="piece"/>, standing on <paramref name="cell"/> of <paramref name="grid"/> in
/// <paramref name="state"/>, is where its own promotion may be taken.
/// </summary>
public delegate bool PromotionZone(GameState state, SquareGridTopology grid, Piece piece, CellId cell);
