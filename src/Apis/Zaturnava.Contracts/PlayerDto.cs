using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player who has signed in: the id that is theirs here, their name, and the services they may sign in with
/// (<see cref="GameRoutes.Logins"/>).
/// </summary>
public sealed record PlayerDto(string Id, string Name, ImmutableList<string>? Services = null)
{
    /// <summary>
    /// The page's settings the player keeps, by name (<see cref="GameRoutes.Preferences"/>), so that every browser
    /// they sign in on starts from them; only for the player themselves.
    /// </summary>
    public ImmutableSortedDictionary<string, string> Preferences { get; init; } =
        ImmutableSortedDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal);

    /// <summary>Whether the other is the same player, with the same services and settings kept.</summary>
    public bool Equals(PlayerDto? other) =>
        other is not null
        && this.Id == other.Id
        && this.Name == other.Name
        && (this.Services ?? []).SequenceEqual(other.Services ?? [])
        && this.Preferences.Count == other.Preferences.Count
        && this.Preferences.All(setting => other.Preferences.GetValueOrDefault(setting.Key) == setting.Value);

    public override int GetHashCode() => HashCode.Combine(this.Id, this.Name);
}
