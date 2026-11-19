namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// A capture by custody drawn on a <see cref="MoveDiagram"/>: the piece moves to <paramref name="Landing"/>, next to
/// an enemy on <paramref name="Taken"/>, which a piece of its own side on <paramref name="Partner"/>, just beyond on
/// the same line, holds with it; the enemy is taken.
/// </summary>
public sealed record CustodyExample(int Landing, int Taken, int Partner);
