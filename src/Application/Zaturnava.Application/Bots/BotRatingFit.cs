using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>
/// Elo ratings fitted to the games bots played each other, as <c>scripts/rate-bots.py</c> fits them: Bradley-Terry
/// strengths (which Elo's expected score is), by minorization-maximization, a draw counting half a win each way, and
/// one draw added between each pair played, so that a bot that never loses still gets a finite rating; anchored at
/// <see cref="AnchorRating"/> for one bot, so that a new bot does not move the others.
/// </summary>
public static class BotRatingFit
{
    /// <summary>The anchor bot's rating.</summary>
    public const double AnchorRating = 1000;

    /// <summary>The most rounds of fitting, should the strengths not settle sooner.</summary>
    public const int MostRounds = 10_000;

    private const double Settled = 1e-12;

    private static readonly double Scale = 400 / Math.Log(10);

    /// <summary>
    /// Each bot's rating, from <paramref name="matchups"/> (of one ruleset); none, if <paramref name="anchor"/> played
    /// in none of them.
    /// </summary>
    public static ImmutableDictionary<string, int> Fitted(
        [Required] string anchor,
        [Required] ImmutableList<BotMatchup> matchups,
        int mostRounds = MostRounds
    )
    {
        ImmutableList<string> bots =
        [
            .. matchups
                .SelectMany(matchup => (string[])[matchup.First, matchup.Second])
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
        if (!bots.Contains(anchor))
        {
            return [];
        }

        ImmutableDictionary<string, double> wins = bots.ToImmutableDictionary(
            bot => bot,
            bot => matchups.Sum(matchup => WinsOf(matchup, bot))
        );
        ImmutableDictionary<string, double> strengths = bots.ToImmutableDictionary(bot => bot, _ => 1.0);
        bool isSettled = false;
        for (int round = 0; round < mostRounds && !isSettled; round++)
        {
            ImmutableDictionary<string, double> next = Next(bots, wins, strengths, matchups, anchor);
            isSettled = bots.All(bot => Math.Abs(next[bot] - strengths[bot]) < Settled);
            strengths = next;
        }

        return strengths.ToImmutableDictionary(
            strength => strength.Key,
            strength => (int)Math.Round(AnchorRating + (Scale * Math.Log(strength.Value)), MidpointRounding.ToEven)
        );
    }

    /// <summary>
    /// The rating of <paramref name="bot"/> that best explains its games in <paramref name="matchups"/> against bots
    /// rated already, their ratings held as <paramref name="rated"/> says (Proposed, 2026-11-13: so that a new bot is
    /// rated on the scale the others are, not by a fit of every bot to a few games each, which draws every rating
    /// toward the anchor); one draw added against each, as <see cref="Fitted"/> adds. Games against bots not rated are
    /// left out; none, if it played none of the rated.
    /// </summary>
    public static int? Against(
        [Required] string bot,
        [Required] ImmutableDictionary<string, int> rated,
        [Required] ImmutableList<BotMatchup> matchups
    )
    {
        ImmutableList<(double Wins, int Games, int Opponent)> played =
        [
            .. matchups
                .Where(matchup => matchup.First == bot || matchup.Second == bot)
                .Where(matchup => rated.ContainsKey(OtherOf(matchup, bot)))
                .Select(matchup => (WinsOf(matchup, bot), matchup.Played + 1, rated[OtherOf(matchup, bot)])),
        ];
        if (played.IsEmpty)
        {
            return null;
        }

        // The wins expected rise with the rating: halve the range until they meet the wins had.
        double low = AnchorRating - 4000;
        double high = AnchorRating + 6000;
        while (high - low > 0.01)
        {
            double middle = (low + high) / 2;
            double expected = played.Sum(each => each.Games / (1 + Math.Pow(10, (each.Opponent - middle) / 400)));
            (low, high) = expected < played.Sum(each => each.Wins) ? (middle, high) : (low, middle);
        }

        return (int)Math.Round((low + high) / 2, MidpointRounding.ToEven);
    }

    /// <summary>One round of fitting: each bot's strength anew, the anchor's then 1.</summary>
    private static ImmutableDictionary<string, double> Next(
        ImmutableList<string> bots,
        ImmutableDictionary<string, double> wins,
        ImmutableDictionary<string, double> strengths,
        ImmutableList<BotMatchup> matchups,
        string anchor
    )
    {
        ImmutableDictionary<string, double> next = bots.ToImmutableDictionary(
            bot => bot,
            bot => wins[bot] / matchups
                .Where(matchup => matchup.First == bot || matchup.Second == bot)
                .Sum(matchup => (matchup.Played + 1) / (strengths[bot] + strengths[OtherOf(matchup, bot)]))
        );
        double anchored = next[anchor];
        return next.ToImmutableDictionary(strength => strength.Key, strength => strength.Value / anchored);
    }

    /// <summary>The bot the other plays in the matchup, one of the two.</summary>
    private static string OtherOf(BotMatchup matchup, string bot) =>
        matchup.First == bot ? matchup.Second : matchup.First;

    /// <summary>The bot's wins in the matchup, a draw half a win, with the one draw added.</summary>
    private static double WinsOf(BotMatchup matchup, string bot) =>
        matchup.First == bot ? matchup.Won + ((matchup.Drawn + 1) / 2.0)
        : matchup.Second == bot ? matchup.Lost + ((matchup.Drawn + 1) / 2.0)
        : 0;
}
