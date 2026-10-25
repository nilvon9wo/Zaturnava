namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A bot that can take a seat: <see cref="Id"/> to seat it by, then a name and description for people
/// choosing, and whether it uses AI (rather than a plain algorithm).
/// </summary>
public sealed record BotDto(string Id, string Name, string Description, bool UsesAi);
