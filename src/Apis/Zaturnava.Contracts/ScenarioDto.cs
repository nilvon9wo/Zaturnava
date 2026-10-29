namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game set up part-way, to start from when trying out a corner of the rules: <see cref="Id"/> to start it
/// by, its ruleset, and a name and description for people choosing. Offered only by a server in development.
/// </summary>
public sealed record ScenarioDto(string Id, string RulesetId, string Name, string Description);
