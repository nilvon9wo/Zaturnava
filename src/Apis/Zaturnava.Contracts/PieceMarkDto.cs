namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// How a piece is marked, where its name's first letter would not tell it apart: by its class's emblem
/// (<c>leader</c>, <c>infantry</c>, <c>heavy</c>, <c>cavalry</c> or <c>artillery</c>; a client draws any other as
/// nothing) and the letters that tell it from the others of its class ("W", "Sh").
/// </summary>
public sealed record PieceMarkDto(string Emblem, string Letters);
