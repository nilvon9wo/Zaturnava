using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A person who has signed in: the name they are shown by, and every login that is theirs; and the page's settings
/// they keep, by name (<see cref="PlayerService.Prefer"/>), so that every browser they sign in on starts from them;
/// and what their profile shows anyone (<see cref="PlayerService.Describe"/>).
/// </summary>
public sealed record Player(PlayerId Id, string Name, ImmutableList<Login> Logins)
{
    public static readonly ImmutableSortedDictionary<string, string> NoPreferences =
        ImmutableSortedDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal);

    public static readonly ImmutableSortedSet<string> NoneShown =
        ImmutableSortedSet.Create<string>(StringComparer.Ordinal);

    public ImmutableSortedDictionary<string, string> Preferences { get; init; } = NoPreferences;

    /// <summary>The description of themselves their profile shows anyone; none, at first.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The services they chose to have their profile show anyone; none, at first (Proposed). Only those they still
    /// sign in with are shown (<see cref="ShownServices"/>).
    /// </summary>
    public ImmutableSortedSet<string> Shown { get; init; } = NoneShown;

    /// <summary>The services their profile shows anyone: those they chose to that they still sign in with.</summary>
    public ImmutableList<string> ShownServices =>
        [.. this.Logins.Select(login => login.Provider).Distinct().Where(this.Shown.Contains)];
}
