using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Whether <paramref name="move"/>, which its piece's kind allows, may be made on <paramref name="grid"/>.
/// </summary>
public delegate bool MoveAllowing(SquareGridTopology grid, Move move);
