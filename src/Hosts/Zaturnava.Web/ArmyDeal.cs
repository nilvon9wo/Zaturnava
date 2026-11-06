namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How a game whose armies are dealt asks for them: every army alike, if <paramref name="AreAlike"/>; or, if
/// <paramref name="Of"/> names a game, that game's again.
/// </summary>
internal sealed record ArmyDeal(bool AreAlike, string? Of = null);
