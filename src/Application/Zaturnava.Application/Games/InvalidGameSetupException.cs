namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class InvalidGameSetupException(string message) : GameRequestException(message)
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
