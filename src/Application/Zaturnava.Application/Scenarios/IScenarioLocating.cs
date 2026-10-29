using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>The scenarios a host offers to start games from: none, as a rule, but while developing.</summary>
public interface IScenarioLocating
{
    /// <summary>Every scenario, in order of id.</summary>
    ImmutableList<Scenario> All { get; }

    /// <exception cref="UnknownScenarioException">There is no such scenario.</exception>
    Scenario Find([Required] string id);
}
