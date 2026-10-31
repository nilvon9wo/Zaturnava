namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>A limit on a piece, told for people, and what lifting it costs; none, if it cannot be lifted.</summary>
public sealed record Restriction(RestrictionKind Kind, string Description, int? Lift = null);
