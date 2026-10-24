using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

public sealed class UnknownBotException(string botId) : GameRequestException($"There is no '{botId}' bot.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
