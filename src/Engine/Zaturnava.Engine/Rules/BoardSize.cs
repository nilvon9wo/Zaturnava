namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// The size a game's creator may set its board to (<see cref="IRulesApplying.Sized"/>): <paramref name="Width"/> cells
/// across and <paramref name="Height"/> up (Stated 2026-11-13: a board need not be square), and each kingdom
/// <paramref name="KingdomWidth"/> along its back rank and <paramref name="KingdomDepth"/> deep.
/// </summary>
public sealed record BoardSize(int Width, int Height, int KingdomWidth, int KingdomDepth);
