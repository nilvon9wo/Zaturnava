using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

public sealed class NotSeatedException(GameId id)
    : GameRequestException($"Only a player of game '{id.Value}' may speak at its table.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
