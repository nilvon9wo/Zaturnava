namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// Where a river meets a kingdom that reaches past the middle of the board (specification §2): it matters only
/// for kingdoms more than half the board wide or deep.
/// </summary>
public enum RiverCourse
{
    /// <summary>The river runs straight on, through the kingdom.</summary>
    ThroughKingdoms,

    /// <summary>The river follows the kingdom's outline, so that no move within a kingdom crosses it.</summary>
    AroundKingdoms,
}
