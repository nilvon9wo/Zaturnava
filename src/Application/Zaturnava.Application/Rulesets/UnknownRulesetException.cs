using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Rulesets;

public sealed class UnknownRulesetException(string id) : GameRequestException($"There is no '{id}' ruleset.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
