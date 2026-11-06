namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// How a game whose armies are dealt is dealt them: every army alike, if <paramref name="AreAlike"/> (Stated: only if
/// the game's creator asks); or as the game <paramref name="ArmiesOf"/> was, armies and places, to play it again so.
/// </summary>
public sealed record Dealing(bool AreAlike = false, GameId? ArmiesOf = null)
{
    /// <summary>Each seat its own army, dealt anew.</summary>
    public static Dealing Anew { get; } = new();
}
