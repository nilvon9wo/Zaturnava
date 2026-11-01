using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A code to link a login that was never issued, was used already, or whose time is up.</summary>
public sealed class LinkExpiredException()
    : GameRequestException("That link has run out, or was used already: ask for another, and try again.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
