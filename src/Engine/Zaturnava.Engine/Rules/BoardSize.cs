namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// The size a game's creator may set its board to (<see cref="IRulesApplying.Sized"/>): <paramref name="Side"/> cells
/// on each side, and each kingdom <paramref name="KingdomWidth"/> along its back rank and
/// <paramref name="KingdomDepth"/> deep.
/// </summary>
public sealed record BoardSize(int Side, int KingdomWidth, int KingdomDepth);
