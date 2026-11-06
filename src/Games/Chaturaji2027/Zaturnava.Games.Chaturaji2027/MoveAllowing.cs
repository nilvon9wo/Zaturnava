using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Whether <paramref name="move"/>, which its piece's kind allows, may be made on <paramref name="grid"/>, in
/// <paramref name="state"/>, on the terrain the game was dealt.
/// </summary>
public delegate bool MoveAllowing(GameState state, SquareGridTopology grid, Move move);
