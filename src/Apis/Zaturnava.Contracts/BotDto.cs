using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A bot that can take a seat: <see cref="Id"/> to seat it by, then a name and description for people
/// choosing, whether it uses AI (rather than a plain algorithm), and whether it <see cref="IsPaid"/> for each use
/// (tokens), and so is seated only when chosen.
/// </summary>
public sealed record BotDto(string Id, string Name, string Description, bool UsesAi, bool IsPaid = false)
{
    /// <summary>
    /// Its Elo rating at each ruleset, by the ruleset's id, from games between the bots: 400 more wins about
    /// ten games in eleven. None for a ruleset it has not been rated at.
    /// </summary>
    public ImmutableDictionary<string, int> Ratings { get; init; } = [];

    /// <summary>
    /// The rulesets, by id, it knows how to play, and is offered for; null, from a server that does not say, every one.
    /// </summary>
    public ImmutableList<string>? Rulesets { get; init; }

    /// <summary>
    /// The family of pieces its army is made of ("Makruk", say), so that it is not offered where every army is alike,
    /// which its family would not be (the user's word of 2026-11-20); none, for a bot that plays any pieces.
    /// </summary>
    public string? Family { get; init; }

    /// <summary>Whether it is offered for the ruleset of <paramref name="rulesetId"/>.</summary>
    public bool Plays(string rulesetId) => this.Rulesets?.Contains(rulesetId) ?? true;
}
