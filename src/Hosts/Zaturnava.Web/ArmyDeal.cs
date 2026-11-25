using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How a game whose armies are dealt asks for them: every army alike, if <paramref name="AreAlike"/>; or, if
/// <paramref name="Of"/> names a game, that game's again.
/// </summary>
internal sealed record ArmyDeal(bool AreAlike, string? Of = null)
{
    /// <summary>Whether the players draft their own armies instead, in a game that lets them.</summary>
    public bool IsDrafted { get; init; }

    /// <summary>For armies drafted, what each seat has to spend; the ruleset's own, if none.</summary>
    public int? Budget { get; init; }

    /// <summary>For armies drafted, the seat that alone drafts one army for everyone; none, for each its own.</summary>
    public int? Drafter { get; init; }

    /// <summary>For armies each seat drafts its own, whether in secret from the other teams.</summary>
    public bool IsSecret { get; init; }

    /// <summary>For armies drafted in secret, whether from allies too.</summary>
    public bool IsHiddenFromAllies { get; init; }

    /// <summary>The board's size, in a game whose creator may set it; the ruleset's own, if none.</summary>
    public BoardSizeDto? Board { get; init; }

    /// <summary>
    /// Whether pieces promoted at the far edge are promoted at the edge to their right too, in a game that offers it.
    /// </summary>
    public bool PromotesAtSide { get; init; }

    /// <summary>Whether the game is played without dice, in a game that rolls them; dealt or not.</summary>
    public bool AreDiceOff { get; init; }

    /// <summary>How many idle rounds end the game, if its creator may say; the server's own, if none.</summary>
    public int? IdleRounds { get; init; }

    /// <summary>In a game without dice, the seat chosen to open it; none, for one drawn at random.</summary>
    public int? Opener { get; init; }

    /// <summary>Whether a team with no leader left on the board has lost; dealt or not.</summary>
    public bool IsLostWithoutLeaders { get; init; }

    /// <summary>The bits of the terrain's number its creator chose, in a game that lets them; 0, for none.</summary>
    public int TerrainMask { get; init; }

    /// <summary>How they set the bits of <see cref="TerrainMask"/>.</summary>
    public int TerrainValue { get; init; }
}
