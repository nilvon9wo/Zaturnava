namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A capture by custody, drawn: the piece moves to <paramref name="Landing"/>, next to an enemy on
/// <paramref name="Taken"/>, which a piece of its own side on <paramref name="Partner"/>, just beyond, holds with it.
/// </summary>
public sealed record CustodyDto(int Landing, int Taken, int Partner);
