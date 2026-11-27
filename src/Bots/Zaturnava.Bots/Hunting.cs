namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What the bots that weigh a position as Sage does hunt, and guard against: in an endgame, how much pressing each
/// enemy leader counts (<see cref="Leaders"/>) and each enemy heavy piece (<see cref="Heavies"/>), as shares of the
/// press (<see cref="Endgame.Pressure"/>), and how many of a hunted piece's escapes count (<see cref="Escapes"/>); and
/// whether an enemy leader that could step onto the empty throne of an ally counts, as one onto the bot's own does
/// (<see cref="AlliedThrones"/>). Not tuned with the weights: each way is a bot's own, played against the others to
/// see which wins (the user, 2026-11-27: "we can/should have different bots with different approaches so we have
/// proof instead of speculation").
/// </summary>
public sealed record Hunting(double Leaders, double Heavies, bool AlliedThrones)
{
    /// <summary>The worth that counts as much again as a throne held: a Rook's, about (Proposed).</summary>
    public const int StakeUnit = 5;

    /// <summary>
    /// How many of a hunted piece's escapes count, at most, each as likely as the dice are to let it move: all, and
    /// each for certain, unless set (<see cref="Endgame.Pressure"/>).
    /// </summary>
    public int? Escapes { get; init; }

    /// <summary>
    /// Whether, with <see cref="Escapes"/> set, each escape counts only as safe as the team's pieces that move first
    /// leave it (<see cref="Endgame.Pressure"/>): by distance, the dice and who moves first, together.
    /// </summary>
    public bool Timed { get; init; }

    /// <summary>
    /// Whether a throne not an ally's, held by a leader, counts by what it controls: one, and as many again as the
    /// pieces its owner still has are worth, in <see cref="StakeUnit"/>s; not, it counts one, whatever it controls (the
    /// user, 2026-11-27: seizing a throne seldom matters, "but when it counts, it really can count").
    /// </summary>
    public bool ThronesByStake { get; init; }

    /// <summary>As the bots have hunted since the endgame came (2026-11-25): the enemy's leaders alone.</summary>
    public static Hunting OfLeaders { get; } = new(Leaders: 1, Heavies: 0, AlliedThrones: false);

    /// <summary>
    /// As the user would have it (2026-11-27): "eliminating in on enemy rooks or other heavies should generally take
    /// priority over eliminating enemy leaders since eliminating the leaders doesn't necessarily end the game and
    /// _most_ of them have limited mobility"; and "the most dangerous thing about the enemy leaders ... is their
    /// ability to take the throne". Shares Proposed.
    /// </summary>
    public static Hunting OfHeavies { get; } = new(Leaders: 0.5, Heavies: 1, AlliedThrones: true);
}
