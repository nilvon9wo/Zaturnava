using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A player as anyone may see them (<see cref="GameRoutes.Profile"/>): their name and their links.</summary>
public sealed record ProfileDto(string Id, string Name, ImmutableList<string> Links);
