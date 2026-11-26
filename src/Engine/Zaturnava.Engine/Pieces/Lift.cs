namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// The <see cref="Restriction"/> of <see cref="Piece"/>, as a <see cref="Kind"/>, bought off: it holds only while the
/// piece is still that kind.
/// </summary>
public sealed record Lift(PieceId Piece, PieceKindId Kind, LiftKind Restriction);
