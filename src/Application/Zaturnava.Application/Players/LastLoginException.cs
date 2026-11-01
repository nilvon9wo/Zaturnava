using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The login a player would unlink is the only one left them, without which they could never sign in again: to be
/// rid of it, they delete their account.
/// </summary>
public sealed class LastLoginException(string provider)
    : GameRequestException($"{provider} is the only way left to sign in as this player: link another first.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
