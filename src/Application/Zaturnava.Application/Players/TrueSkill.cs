using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A TrueSkill rating (Herbrich, Minka and Graepel, 2006): a belief about a player's skill, its mean and deviation. A
/// team's performance is the sum of its members'; a game between two sides moves each member's mean by as much as
/// their own uncertainty makes them responsible for, and narrows it (the user's choice of 2026-11-20 to count all three
/// systems; docs/ratings-design.md). A game of more than two teams is rated each team against the rest as one. Shown
/// as <see cref="Conservative"/>, the mean less three deviations, as TrueSkill itself ranks.
/// </summary>
public sealed record TrueSkill(double Mean, double Deviation)
{
    /// <summary>A new player's: 25, unsure by 25 / 3, as the system suggests.</summary>
    public static readonly TrueSkill First = new(25, 25.0 / 3);

    /// <summary>
    /// How far one game's performance strays from skill, by chance: 25 / 6, the system's default (Proposed: dice may
    /// later have more).
    /// </summary>
    public const double Beta = 25.0 / 6;

    /// <summary>How much a skill may drift between games, added to its deviation before each: 25 / 300.</summary>
    private const double Drift = 25.0 / 300;

    /// <summary>
    /// How close two sides' performances must be to draw: of four players, with draws taken as one game in ten
    /// (Proposed), the inverse normal of 0.55, times the square root of four, times <see cref="Beta"/>.
    /// </summary>
    private const double DrawMargin = 0.125661 * 2 * Beta;

    /// <summary>The deviation taken for a bot's rating, which a bot's many games make sure of (Proposed).</summary>
    public const double BotDeviation = 1;

    /// <summary>What it shows: the mean less three deviations, a skill the player very likely has at least.</summary>
    public double Conservative => this.Mean - (3 * this.Deviation);

    /// <summary>A rating on Elo's scale, as TrueSkill: equal odds alike, 200 Elo one <see cref="Beta"/>.</summary>
    public static TrueSkill FromElo(double elo, double deviation) =>
        new(First.Mean + ((elo - Glicko.First.Rating) * Beta / 200), deviation);

    /// <summary>
    /// Every rating at a game after it: <paramref name="side"/> against <paramref name="others"/>, scored 1 if the side
    /// won, a half if drawn, 0 if it lost; the side's ratings, then the others', in the order given.
    /// </summary>
    public static (ImmutableList<TrueSkill> Side, ImmutableList<TrueSkill> Others) After(
        ImmutableList<TrueSkill> side,
        ImmutableList<TrueSkill> others,
        double score
    )
    {
        ImmutableList<TrueSkill> drifted = [.. side.Select(Drifted)];
        ImmutableList<TrueSkill> facing = [.. others.Select(Drifted)];
        double c = Math.Sqrt(
            drifted.Concat(facing).Sum(rating => rating.Deviation * rating.Deviation)
                + ((drifted.Count + facing.Count) * Beta * Beta)
        );
        // as the winner saw it: the side, if it won or drew; else the others
        double sign = score >= 0.5 ? 1 : -1;
        double t = sign * (drifted.Sum(rating => rating.Mean) - facing.Sum(rating => rating.Mean)) / c;
        double e = DrawMargin / c;
        (double v, double w) = score is > 0 and < 1 ? Drawn(t, e) : Won(t, e);
        return (
            [.. drifted.Select(rating => rating.Moved(sign * v, w, c))],
            [.. facing.Select(rating => rating.Moved(-sign * v, w, c))]
        );
    }

    private static TrueSkill Drifted(TrueSkill rating) =>
        rating with { Deviation = Math.Sqrt((rating.Deviation * rating.Deviation) + (Drift * Drift)) };

    private TrueSkill Moved(double v, double w, double c)
    {
        double variance = this.Deviation * this.Deviation;
        return new TrueSkill(
            this.Mean + (variance / c * v),
            Math.Sqrt(variance * (1 - (variance / (c * c) * w)))
        );
    }

    private static (double V, double W) Won(double t, double e)
    {
        double v = Density(t - e) / Cumulative(t - e);
        return (v, v * (v + t - e));
    }

    private static (double V, double W) Drawn(double t, double e)
    {
        double between = Cumulative(e - t) - Cumulative(-e - t);
        double v = (Density(-e - t) - Density(e - t)) / between;
        return (v, (v * v) + ((((e - t) * Density(e - t)) + ((e + t) * Density(e + t))) / between));
    }

    private static double Density(double x) => Math.Exp(-x * x / 2) / Math.Sqrt(2 * Math.PI);

    /// <summary>The normal distribution's, by the complementary error function, close even in its tails.</summary>
    private static double Cumulative(double x) => Complement(-x / Math.Sqrt(2)) / 2;

    /// <summary>The complementary error function, by Chebyshev fitting: within 1.2e-7 of it, relatively.</summary>
    private static double Complement(double x)
    {
        double z = Math.Abs(x);
        double t = 1 / (1 + (z / 2));
        double fitted = t * Math.Exp(
            -(z * z) - 1.26551223 + (t * (1.00002368 + (t * (0.37409196 + (t * (0.09678418 + (t * (-0.18628806
                + (t * (0.27886807 + (t * (-1.13520398 + (t * (1.48851587 + (t * (-0.82215223
                    + (t * 0.17087277)))))))))))))))))
        );
        return x >= 0 ? fitted : 2 - fitted;
    }
}
