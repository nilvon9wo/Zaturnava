namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How much each thing Sage counts in a position weighs (<see cref="Appraisal"/>): the points scored, the
/// pieces on the board, the leaders among them, the prisoners held, the thrones held, the worst the enemy
/// could win next, and the most the team could. What each is worth to winning is for playing to show, not
/// for guessing: these are the starting weights, to be tuned by Sage playing itself.
/// </summary>
public sealed record SageWeights(
    double Score,
    double Material,
    double Leaders,
    double Prisoners,
    double Thrones,
    double Exposure,
    double Chances
)
{
    public static SageWeights Default { get; } = new(
        Score: 1,
        Material: 1,
        Leaders: 2,
        Prisoners: 0.5,
        Thrones: 3,
        Exposure: 1,
        Chances: 0.3
    );
}
