using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>How a page starts a game and sits the player at it: as planned, or at once, at random.</summary>
internal static class GameStarter
{
    /// <summary>
    /// The player at <paramref name="person"/>, and the bots, in the order given, in the other seats -
    /// each at most once; a seat with no bot left is for a person (an empty plan).
    /// </summary>
    public static List<string> Plans(int seatCount, int person, IEnumerable<string> bots)
    {
        Queue<string> left = new(bots);
        return [.. Enumerable.Range(0, seatCount)
            .Select(seat => seat == person || left.Count == 0 ? string.Empty : left.Dequeue())];
    }

    /// <summary>A bot's id for each seat planned for one; null for each seat left to a person.</summary>
    public static ImmutableList<string?> BotsOf(IEnumerable<string> plans) =>
        [.. plans.Select(plan => plan.Length == 0 ? null : plan)];

    /// <summary>The bots that may be seated without anyone choosing them: none that is paid for each use.</summary>
    public static IEnumerable<string> Unpaid(CatalogDto catalog) =>
        catalog.Bots.Where(bot => !bot.IsPaid).Select(bot => bot.Id);

    /// <summary>
    /// The bots a quick game seats, <paramref name="count"/> of them, shuffled: the easiest at the ruleset, by
    /// their ratings, for an easy game; the hardest, for a hard one; any, for a medium one. Never one paid for
    /// each use; one not rated counts as easiest.
    /// </summary>
    public static IEnumerable<string> QuickBots(CatalogDto catalog, string rulesetId, Difficulty difficulty, int count)
    {
        ImmutableList<BotDto> unpaid = [.. catalog.Bots.Where(bot => !bot.IsPaid)];
        ImmutableList<BotDto> easiestFirst = [.. unpaid.OrderBy(bot => bot.Ratings.GetValueOrDefault(rulesetId))];
        IEnumerable<BotDto> chosen = difficulty switch
        {
            Difficulty.Easy => easiestFirst.Take(count),
            Difficulty.Hard => Enumerable.Reverse(easiestFirst).Take(count),
            _ => unpaid.OrderBy(_ => Random.Shared.Next()).Take(count),
        };
        return chosen.Select(bot => bot.Id).OrderBy(_ => Random.Shared.Next());
    }

    /// <summary>
    /// A game at once, as hard as asked (<see cref="QuickBots"/>): the player at a seat chosen at random, the
    /// bots among the others.
    /// </summary>
    public static Task<Seating> Quick(IGameServing games, CatalogDto catalog, Difficulty difficulty)
    {
        RulesetDto ruleset = catalog.Rulesets[0];
        int person = Random.Shared.Next(ruleset.SeatCount);
        IEnumerable<string> bots = QuickBots(catalog, ruleset.Id, difficulty, ruleset.SeatCount - 1);
        return Start(games, ruleset.Id, BotsOf(Plans(ruleset.SeatCount, person, bots)), person, null);
    }

    /// <summary>
    /// Starts a game of the ruleset with <paramref name="bots"/> in their seats, from the start or the
    /// <paramref name="scenario"/>, and sits the player at <paramref name="person"/>; or has them watch, if
    /// that is no seat. A game played again after another (<paramref name="continuing"/>) is at its table.
    /// </summary>
    public static async Task<Seating> Start(
        IGameServing games,
        string rulesetId,
        ImmutableList<string?> bots,
        int person,
        string? scenario,
        string? continuing = null
    )
    {
        CreateGameRequest request = new(rulesetId, bots) { Scenario = scenario, Continuing = continuing };
        GameCreatedDto created = await games.Create(request, CancellationToken.None);
        return person >= 0
            ? await Claim(games, created.GameId, person)
            : Seating.Watching(created.GameId);
    }

    public static async Task<Seating> Claim(IGameServing games, string gameId, int seat)
    {
        SeatClaimedDto claimed = await games.Claim(gameId, seat, CancellationToken.None);
        return new Seating(gameId, new SeatClaim(seat, claimed.Token));
    }
}
