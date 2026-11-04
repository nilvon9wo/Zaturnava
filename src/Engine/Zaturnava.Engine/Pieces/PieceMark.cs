namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// How a kind is marked on its piece, where its name's first letter would not tell it apart: by
/// <paramref name="Emblem"/>, a symbol of what sort of piece it is (Grand Legions: <c>leader</c>, <c>infantry</c>,
/// <c>heavy</c>, <c>cavalry</c> or <c>artillery</c>), and <paramref name="Letters"/>, which tell it from the others
/// of that sort ("W" for the Western Pawn, "Sh" for the Shogi one).
/// </summary>
public sealed record PieceMark(string Emblem, string Letters);
