using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A login being linked to one player is already another's. It is not moved: whoever wants it here signs in with
/// it, deletes that other player, and links it again.
/// </summary>
public sealed class LoginTakenException(string provider)
    : GameRequestException($"That {provider} account is already another player's here.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
