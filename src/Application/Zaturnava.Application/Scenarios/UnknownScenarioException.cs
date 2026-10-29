using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

public sealed class UnknownScenarioException(string id) : GameRequestException($"There is no '{id}' scenario.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
