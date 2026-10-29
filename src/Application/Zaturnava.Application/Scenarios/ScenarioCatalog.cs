using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

public sealed class ScenarioCatalog(ImmutableList<Scenario> scenarios) : IScenarioLocating
{
    /// <summary>No scenarios: what a host offers but while developing.</summary>
    public static readonly ScenarioCatalog None = new([]);

    public ImmutableList<Scenario> All { get; } =
        [.. scenarios.OrderBy(scenario => scenario.Id, StringComparer.Ordinal)];

    public Scenario Find([Required] string id) =>
        this.All.FirstOrDefault(scenario => scenario.Id == id) ?? throw new UnknownScenarioException(id);
}
