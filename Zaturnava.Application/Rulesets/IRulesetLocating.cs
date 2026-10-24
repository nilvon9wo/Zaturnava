using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Rulesets;

/// <summary>The rulesets - variants - a host offers. Adding one is registering it, nothing more.</summary>
public interface IRulesetLocating
{
    ImmutableList<IRulesApplying> All { get; }

    /// <exception cref="UnknownRulesetException">There is no such ruleset.</exception>
    IRulesApplying Find([Required] string id);
}
