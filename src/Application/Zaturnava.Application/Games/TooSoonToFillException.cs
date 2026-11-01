namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class TooSoonToFillException(GameId id)
    : GameRequestException($"Game '{id.Value}' still waits for people to take its open seats: wait a little longer.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
