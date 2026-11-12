namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How a game whose armies are dealt asks for them: every army alike, if <paramref name="AreAlike"/>; or, if
/// <paramref name="Of"/> names a game, that game's again.
/// </summary>
internal sealed record ArmyDeal(bool AreAlike, string? Of = null)
{
    /// <summary>Whether the players draft their own armies instead, in a game that lets them.</summary>
    public bool IsDrafted { get; init; }
}
