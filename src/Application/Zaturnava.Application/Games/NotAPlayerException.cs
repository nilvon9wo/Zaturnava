namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class NotAPlayerException(GameId id)
    : GameRequestException($"Only a player of game '{id.Value}' may fill its open seats.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
