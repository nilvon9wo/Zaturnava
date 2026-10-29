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
}
