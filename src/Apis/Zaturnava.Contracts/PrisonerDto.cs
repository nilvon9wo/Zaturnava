namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A captured piece: its kind, its name for people, and the seat it belongs to.</summary>
public sealed record PrisonerDto(int Id, string Kind, string Name, int Owner);
