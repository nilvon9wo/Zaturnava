namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// Something only a server offering scenarios (a development server) does was asked of one that offers none.
/// </summary>
public sealed class ScenariosNotOfferedException()
    : GameRequestException("Only a development server, offering scenarios, does this.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
