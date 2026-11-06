namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// How a kind is marked on its piece, where its name's first letter would not tell it apart: by
/// <paramref name="Emblem"/>, the symbol it is drawn with (Grand Legions: <c>rook</c>, <c>camel</c>, <c>pawn</c>...),
/// and <paramref name="Letters"/>, which tell it from the others sharing that symbol ("W" for the Western Pawn, "Sh"
/// for the Shogi one); none, for a kind whose symbol is its own.
/// </summary>
public sealed record PieceMark(string Emblem, string Letters)
{
    /// <summary>
    /// The emblem of the kind's class, drawn beneath its symbol (Grand Legions: <c>leader</c>, <c>infantry</c>,
    /// <c>heavy</c>, <c>cavalry</c> or <c>artillery</c>); none, for a game whose pieces have no classes.
    /// </summary>
    public string? Class { get; init; }
}
