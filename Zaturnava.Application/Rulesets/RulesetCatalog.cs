using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Rulesets;

public sealed class RulesetCatalog(ImmutableList<IRulesApplying> rulesets) : IRulesetLocating
{
    public ImmutableList<IRulesApplying> All { get; } = rulesets;

    public IRulesApplying Find([Required] string id) =>
        this.All.FirstOrDefault(rules => rules.Id == id) ?? throw new UnknownRulesetException(id);
}
