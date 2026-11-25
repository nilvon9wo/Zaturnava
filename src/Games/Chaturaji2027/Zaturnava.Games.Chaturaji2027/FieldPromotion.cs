using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Promotion in the field (asked 2026-11-25): a piece, not a leader, that has stood on an enemy throne at the end
/// of <paramref name="Turns"/> turns of the game together may, if its player chooses, become <paramref name="Into"/>,
/// a leader, for its own army, free; and its player then scores as for a leader entering an enemy throne.
/// </summary>
public sealed record FieldPromotion(PieceKindId Into, int Turns);
