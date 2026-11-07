using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A person who has signed in: the name they are shown by, and every login that is theirs; and the page's settings
/// they keep, by name (<see cref="PlayerService.Prefer"/>), so that every browser they sign in on starts from them.
/// </summary>
public sealed record Player(PlayerId Id, string Name, ImmutableList<Login> Logins)
{
    public static readonly ImmutableSortedDictionary<string, string> NoPreferences =
        ImmutableSortedDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal);

    public ImmutableSortedDictionary<string, string> Preferences { get; init; } = NoPreferences;
}
