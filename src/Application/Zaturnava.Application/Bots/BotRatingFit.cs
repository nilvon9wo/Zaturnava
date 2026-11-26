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
    /// Every bot's rating not held in <paramref name="held"/>, fitted together from all of <paramref name="matchups"/>:
    /// their games against the bots held, whose ratings stay as they are (Proposed, 2026-11-13: so that a new bot is
    /// rated on the scale the others are), and against each other (2026-11-25: rated against the held alone, a bot that
    /// beats Random and loses to Midas fits any rating between, so the weakest bots rated out of order). One draw added
    /// for each bot, against one rated <see cref="AnchorRating"/>, so that a bot that never loses still gets a finite
    /// rating (2026-11-26: a draw added against every bot it played, with only two games a pair, gave a bot that lost
    /// every game to the strongest a half win against each, so that the weakest rated near Midas). A bot joined to no
    /// held bot, by any chain of games, is left out.
    /// </summary>
    public static ImmutableDictionary<string, int> Around(
        [Required] ImmutableDictionary<string, int> held,
        [Required] ImmutableList<BotMatchup> matchups,
        int mostRounds = MostRounds
    )
    {
        ImmutableList<string> free =
        [
            .. Connected(held, matchups).Where(bot => !held.ContainsKey(bot)).Order(StringComparer.Ordinal),
        ];
        ImmutableList<BotMatchup> counted =
            [.. matchups.Where(matchup => IsCounted(matchup.First) && IsCounted(matchup.Second))];
        ImmutableDictionary<string, double> wins = free.ToImmutableDictionary(
            bot => bot,
            bot => PriorDraw + counted.Sum(matchup => PlayedWinsOf(matchup, bot))
        );
        ImmutableDictionary<string, double> strengths = held
            .ToImmutableDictionary(rating => rating.Key, rating => Math.Exp((rating.Value - AnchorRating) / Scale))
            .AddRange(free.Select(bot => KeyValuePair.Create(bot, 1.0)));
        bool isSettled = false;
        for (int round = 0; round < mostRounds && !isSettled; round++)
        {
            ImmutableDictionary<string, double> next = NextAround(free, wins, strengths, counted);
            isSettled = IsSettled(free, strengths, next);
            strengths = next;
        }

        return free.ToImmutableDictionary(
            bot => bot,
            bot => (int)Math.Round(AnchorRating + (Scale * Math.Log(strengths[bot])), MidpointRounding.ToEven)
        );

        bool IsCounted(string bot) => held.ContainsKey(bot) || free.Contains(bot);
    }

    /// <summary>The half win of the one draw added for each bot fitted around the held.</summary>
    private const double PriorDraw = 0.5;

    /// <summary>
    /// The strength of the one each bot fitted around the held is drawn against: <see cref="AnchorRating"/>'s.
    /// </summary>
    private const double PriorStrength = 1;

    /// <summary>
    /// One round of fitting around the held: each free bot's strength anew, by its games and its one added draw, the
    /// held as they were.
    /// </summary>
    private static ImmutableDictionary<string, double> NextAround(
        ImmutableList<string> free,
        ImmutableDictionary<string, double> wins,
        ImmutableDictionary<string, double> strengths,
        ImmutableList<BotMatchup> counted
    ) =>
        strengths.SetItems(
            free.Select(bot => KeyValuePair.Create(
                bot,
                wins[bot] / (
                    (1 / (strengths[bot] + PriorStrength))
                    + counted
                        .Where(matchup => matchup.First == bot || matchup.Second == bot)
                        .Sum(matchup => matchup.Played / (strengths[bot] + strengths[OtherOf(matchup, bot)]))
                )
            ))
        );

    /// <summary>The bot's wins in the matchup, a draw half a win, as played.</summary>
    private static double PlayedWinsOf(BotMatchup matchup, string bot) =>
        matchup.First == bot ? matchup.Won + (matchup.Drawn / 2.0)
        : matchup.Second == bot ? matchup.Lost + (matchup.Drawn / 2.0)
        : 0;

    /// <summary>Whether no free bot's strength moved by more than a hair, as a share of it.</summary>
    private static bool IsSettled(
        ImmutableList<string> free,
        ImmutableDictionary<string, double> before,
        ImmutableDictionary<string, double> after
    ) =>
        free.All(bot => Math.Abs(after[bot] - before[bot]) < Settled * before[bot]);

    /// <summary>The bots joined to a held bot by a chain of games, the held among them.</summary>
    private static ImmutableHashSet<string> Connected(
        ImmutableDictionary<string, int> held,
        ImmutableList<BotMatchup> matchups
    ) =>
        Reached([.. held.Keys], matchups);

    /// <summary>The bots <paramref name="reached"/>, and every bot joined to one of them by a chain of games.</summary>
    private static ImmutableHashSet<string> Reached(
        ImmutableHashSet<string> reached,
        ImmutableList<BotMatchup> matchups
    )
    {
        ImmutableHashSet<string> grown = reached.Union(
            matchups
                .Where(matchup => reached.Contains(matchup.First) || reached.Contains(matchup.Second))
                .SelectMany(matchup => (string[])[matchup.First, matchup.Second])
        );
        return grown.Count > reached.Count ? Reached(grown, matchups) : reached;
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
