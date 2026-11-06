namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// How a piece is marked, where its name's first letter would not tell it apart: by the symbol it is drawn with
/// (<c>rook</c>, <c>camel</c>, <c>pawn</c>...; a client draws one it does not know as nothing) and the letters that
/// tell it from the others sharing that symbol ("W", "Sh"); empty, for a piece whose symbol is its own.
/// </summary>
public sealed record PieceMarkDto(string Emblem, string Letters)
{
    /// <summary>
    /// The emblem of the piece's class, drawn beneath its symbol, beside its letters (<c>leader</c>, <c>infantry</c>,
    /// <c>heavy</c>, <c>cavalry</c> or <c>artillery</c>); none, for a game whose pieces have no classes.
    /// </summary>
    public string? Class { get; init; }
}
