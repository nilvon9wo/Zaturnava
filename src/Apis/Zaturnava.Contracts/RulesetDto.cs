namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A ruleset a game can be created with, and how many seats it has.</summary>
public sealed record RulesetDto(string Id, string Name, int SeatCount);
