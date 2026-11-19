namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>How many games started from scenarios were let go of (<see cref="GameRoutes.ScenarioGames"/>).</summary>
public sealed record ScenarioGamesRemovedDto(int Removed);
