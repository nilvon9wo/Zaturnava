namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class GameNotFoundException(GameId id) : GameRequestException($"There is no game '{id.Value}'.")
{
    public override GameRequestFailure Failure => GameRequestFailure.NotFound;
}
