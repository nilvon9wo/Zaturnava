namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A player as anyone may see them (<see cref="GameRoutes.Profile"/>): their name.</summary>
public sealed record ProfileDto(string Id, string Name);
