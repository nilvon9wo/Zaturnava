namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A player who has signed in: the id that is theirs here, and their name.</summary>
public sealed record PlayerDto(string Id, string Name);
