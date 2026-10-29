using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>A scenario names what its ruleset has none of, or is for another ruleset.</summary>
public sealed class InvalidScenarioException(string message) : GameRequestException(message)
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
