using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// A Glicko-2 rating (Glickman, 2001): the rating, on Elo's scale; its deviation, how unsure it is; and its volatility,
/// how erratic its player's results are. Each game is a rating period of its own. A team game is rated, as the
/// published system has no teams, by an approximation (the user's choice of 2026-11-20 to count all three systems;
/// docs/ratings-design.md): each player's expected score is their team's mean rating against each opposing team's, and
/// each opposing team counts as one opponent of that mean rating and the root-mean-square of its deviations; the
/// result moves the player's own rating, by their own deviation.
/// </summary>
public sealed record Glicko(double Rating, double Deviation, double Volatility)
{
    /// <summary>A new player's: 1500, unsure by 350, volatile by 0.06, as the system suggests.</summary>
    public static readonly Glicko First = new(1500, 350, 0.06);

    /// <summary>The deviation taken for a bot's rating, which a bot's many games make sure of (Proposed).</summary>
    public const double BotDeviation = 50;

    /// <summary>How fast volatility may change: the system's suggested 0.3 to 1.2; 0.5 (Proposed).</summary>
    private const double Tau = 0.5;

    private const double Scale = 173.7178;
    private const double Tolerance = 0.000001;

    /// <summary>
    /// The rating after one rating period: <paramref name="team"/> the mean rating of the player's team (theirs among
    /// them; for a player alone, their own), against each of <paramref name="opponents"/>, an opposing team as one,
    /// scored 1 for a win, a half a draw, 0 a loss.
    /// </summary>
    public Glicko After(double team, ImmutableList<(Glicko Opponent, double Score)> opponents)
    {
        double mu = (team - First.Rating) / Scale;
        double phi = this.Deviation / Scale;
        ImmutableList<(double G, double E, double Score)> faced =
        [
            .. opponents.Select(one =>
            {
                double g = G(one.Opponent.Deviation / Scale);
                double e = 1 / (1 + Math.Exp(-g * (mu - ((one.Opponent.Rating - First.Rating) / Scale))));
                return (g, e, one.Score);
            }),
        ];
        double v = 1 / faced.Sum(one => one.G * one.G * one.E * (1 - one.E));
        double gained = faced.Sum(one => one.G * (one.Score - one.E));
        double volatility = this.NewVolatility(phi, v, v * gained);
        double before = Math.Sqrt((phi * phi) + (volatility * volatility));
        double after = 1 / Math.Sqrt((1 / (before * before)) + (1 / v));
        return new Glicko(this.Rating + (Scale * after * after * gained), Scale * after, volatility);
    }

    /// <summary>One opposing team, as one opponent: its mean rating, and its deviations' root mean square.</summary>
    public static Glicko TeamOf(ImmutableList<Glicko> seats) =>
        new(
            seats.Average(seat => seat.Rating),
            Math.Sqrt(seats.Average(seat => seat.Deviation * seat.Deviation)),
            First.Volatility
        );

    private static double G(double phi) => 1 / Math.Sqrt(1 + (3 * phi * phi / (Math.PI * Math.PI)));

    /// <summary>The new volatility, by the system's Illinois iteration.</summary>
    private double NewVolatility(double phi, double v, double delta)
    {
        double a = Math.Log(this.Volatility * this.Volatility);
        double F(double x)
        {
            double ex = Math.Exp(x);
            double spread = (phi * phi) + v + ex;
            return (ex * ((delta * delta) - (phi * phi) - v - ex) / (2 * spread * spread)) - ((x - a) / (Tau * Tau));
        }

        double low = a;
        double high = delta * delta > (phi * phi) + v
            ? Math.Log((delta * delta) - (phi * phi) - v)
            : Enumerable.Range(1, int.MaxValue).Select(k => a - (k * Tau)).First(x => F(x) >= 0);
        (double fLow, double fHigh) = (F(low), F(high));
        while (Math.Abs(high - low) > Tolerance)
        {
            double middle = low + ((low - high) * fLow / (fHigh - fLow));
            double fMiddle = F(middle);
            if (fMiddle * fHigh <= 0)
            {
                (low, fLow) = (high, fHigh);
            }
            else
            {
                fLow /= 2;
            }

            (high, fHigh) = (middle, fMiddle);
        }

        return Math.Exp(low / 2);
    }
}
