using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>
/// A bot as it is offered to whoever seats one: <see cref="Id"/> to ask for it by, a <see cref="Name"/>
/// and <see cref="Description"/> for people, and whether it <see cref="UsesAi"/> (a language model or
/// any other inference) rather than a plain algorithm. A bot that <see cref="IsPaid"/> costs whoever
/// supplied its key for each use (tokens): it is never seated but by someone choosing it.
/// </summary>
public sealed record BotProfile(string Id, string Name, string Description, bool UsesAi, bool IsPaid = false)
{
    /// <summary>
    /// How strongly it plays each ruleset, by the ruleset's id: its Elo rating, from games between the bots;
    /// none for a ruleset it has not been rated at.
    /// </summary>
    public ImmutableDictionary<string, int> Ratings { get; init; } = [];

    /// <summary>
    /// The rulesets, by id, it knows how to play (<see cref="IBotPlaying.Knows"/>), and so is offered for; null, if
    /// not told, for every one.
    /// </summary>
    public ImmutableHashSet<string>? Rulesets { get; init; }

    /// <summary><see cref="Rulesets"/> in order of id, as a list to send; null, if not told.</summary>
    public ImmutableList<string>? RulesetsInOrder =>
        this.Rulesets is { } known ? [.. known.Order(StringComparer.Ordinal)] : null;

    /// <summary>Whether it is offered for the ruleset of <paramref name="rulesetId"/>.</summary>
    public bool Plays(string rulesetId) => this.Rulesets?.Contains(rulesetId) ?? true;
}
