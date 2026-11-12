using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A player as anyone may see them (<see cref="GameRoutes.Profile"/>): their name, the description they give of
/// themselves, and those of the services they sign in with that they choose to show.
/// </summary>
public sealed record ProfileDto(string Id, string Name)
{
    public string Description { get; init; } = string.Empty;

    public ImmutableList<string> Services { get; init; } = [];
}
