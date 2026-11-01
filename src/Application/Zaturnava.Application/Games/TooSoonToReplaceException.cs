namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class TooSoonToReplaceException(GameId id)
    : GameRequestException(
        $"No one at game '{id.Value}' may be replaced by a bot yet, at least not by you: wait a little longer."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
