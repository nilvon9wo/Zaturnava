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
