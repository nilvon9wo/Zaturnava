using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// How a game whose armies are dealt is dealt them: every army alike, if <paramref name="AreAlike"/> (Stated: only if
/// the game's creator asks); or as the game <paramref name="ArmiesOf"/> was, armies and places, to play it again so.
/// </summary>
public sealed record Dealing(bool AreAlike = false, GameId? ArmiesOf = null)
{
    /// <summary>
    /// How the players draft their own armies, if they do and the ruleset lets them (it is ignored by one that does
    /// not): their budget and their one drafter, if set; the terrain is dealt all the same. None, if they do not.
    /// </summary>
    public DraftSettings? Drafting { get; init; }

    /// <summary>
    /// The board's size its creator set, for a ruleset that lets them (<see cref="IRulesApplying.SizableBoard"/>);
    /// none, for the ruleset's own.
    /// </summary>
    public BoardSize? Board { get; init; }

    /// <summary>
    /// Whether pieces promoted at the far edge are promoted at the edge to their right too, for a ruleset that lets
    /// its creator say so (<see cref="IRulesApplying.MayPromoteAtSide"/>; it is ignored by one that does not).
    /// </summary>
    public bool PromotesAtSide { get; init; }

    /// <summary>
    /// What each army dealt, not drafted, spends, if its creator set it (at least 1); none, for what the ruleset gives.
    /// </summary>
    public int? Budget { get; init; }

    /// <summary>
    /// Whether the game is played without dice (<see cref="GameState.AreDiceOff"/>), for a ruleset that rolls them
    /// (<see cref="IRulesApplying.HasDice"/>; it is ignored by one that does not), dealt or not, but never from a
    /// scenario, which has dice of its own.
    /// </summary>
    public bool AreDiceOff { get; init; }

    /// <summary>
    /// How many idle rounds end the game (<see cref="GameState.IdleRounds"/>), if its creator set them (at least 1),
    /// for a ruleset that lets them (<see cref="IRulesApplying.IdleRounds"/>; it is ignored by one that does
    /// not); dealt or not, but never from a scenario. None, for the server's own.
    /// </summary>
    public int? IdleRounds { get; init; }

    /// <summary>
    /// For a game without dice, the seat its creator chose to open it; none, for one drawn at random.
    /// </summary>
    public int? Opener { get; init; }

    /// <summary>
    /// Whether a team with no leader left on the board has lost (<see cref="GameState.IsLostWithoutLeaders"/>);
    /// dealt or not, but never from a scenario.
    /// </summary>
    public bool IsLostWithoutLeaders { get; init; }

    /// <summary>
    /// How many seconds each player, anonymous or signed in, may be idle deciding before their turn passes, if its
    /// creator set it (from <see cref="GameService.LeastIdleSeconds"/> to <see cref="GameService.MostIdleSeconds"/>);
    /// dealt or not, but never from a scenario. None, to wait on them as every game does.
    /// </summary>
    public int? IdleSeconds { get; init; }

    /// <summary>
    /// What of the terrain its creator chose, for a ruleset that lets them (<see cref="IRulesApplying.TerrainAxes"/>;
    /// any other bits are ignored), the rest dealt at random; none, for all of it dealt.
    /// </summary>
    public TerrainChoice? Terrain { get; init; }

    /// <summary>Each seat its own army, dealt anew.</summary>
    public static Dealing Anew { get; } = new();
}
