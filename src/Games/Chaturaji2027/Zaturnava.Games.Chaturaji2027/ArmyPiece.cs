using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>A piece of seat 0's army: its kind, and where it starts, row 0 at the bottom.</summary>
public sealed record ArmyPiece(PieceKindId Kind, int Column, int Row);
