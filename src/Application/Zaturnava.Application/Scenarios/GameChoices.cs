namespace Net.NowhereAtAll.Zaturnava.Application.Scenarios;

/// <summary>
/// How a game's creator set it up, beyond its armies, recorded in its first action (<see cref="ScenarioStart"/>):
/// played without dice, if <paramref name="AreDiceOff"/>, and opened then by the seat <paramref name="Opener"/>
/// (the first, if none); ending after <paramref name="IdleRounds"/> idle rounds, if set (the server's own, if not);
/// a team with no leader left losing, if <paramref name="IsLostWithoutLeaders"/>; and each player's turn passing once
/// they have been idle <paramref name="IdleSeconds"/> seconds, if set.
/// </summary>
public sealed record GameChoices(
    bool AreDiceOff = false,
    int? IdleRounds = null,
    int? Opener = null,
    bool IsLostWithoutLeaders = false,
    int? IdleSeconds = null
)
{
    /// <summary>A game set up as every game is.</summary>
    public static GameChoices None { get; } = new();

    /// <summary>Whether it is set up as every game is.</summary>
    public bool IsNone => this == None;
}
