using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Moves;

/// <summary>
/// A move straight ahead, as <see cref="Piece"/>'s army faces, of two cells or more: from <see cref="From"/>, over
/// <see cref="Passed"/>, to <see cref="To"/>; what <i>en passant</i> may take.
/// </summary>
public sealed record Passage(Piece Piece, CellId From, ImmutableList<CellId> Passed, CellId To);
