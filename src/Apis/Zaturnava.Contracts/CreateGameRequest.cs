using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A new game: its ruleset, and for each seat in order, the bot that plays it, or null to leave it
/// open for a person to claim. Both are required: a request without them is refused as it is read.
/// </summary>
public sealed record CreateGameRequest([Required] string RulesetId, [Required] ImmutableList<string?> Bots)
{
    /// <summary>The scenario to start from (<see cref="CatalogDto.Scenarios"/>); null to start a new game.</summary>
    public string? Scenario { get; init; }

    /// <summary>
    /// The game this one is played again after, if any: the new game is at the same table, so what was said there
    /// goes on.
    /// </summary>
    public string? Continuing { get; init; }
}
