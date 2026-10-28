using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>A kind of piece is data: the movement rules it may use, nothing else.</summary>
public sealed record PieceKind(PieceKindId Id, ImmutableList<MovementRule> Movement);
