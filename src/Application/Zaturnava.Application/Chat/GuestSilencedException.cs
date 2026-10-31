using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

public sealed class GuestSilencedException(GameId id)
    : GameRequestException(
        $"Only a player who signed in before taking their seat may speak at the table of game '{id.Value}'; "
        + "a guest may read what is said."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
