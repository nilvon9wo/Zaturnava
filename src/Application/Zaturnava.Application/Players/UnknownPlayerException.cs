using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A player's token that stands for no one: signed out, deleted, or never given. Refused, rather than taken as a
/// guest's, so that whoever sent it learns they are not signed in.
/// </summary>
public sealed class UnknownPlayerException()
    : GameRequestException("That player token stands for no one, now: sign in again, or play as a guest.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
