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
    /// A game at once: the player at a seat chosen at random, the bots shuffled among the others - never one
    /// paid for each use.
    /// </summary>
    public static Task<Seating> Quick(IGameServing games, CatalogDto catalog)
    {
        RulesetDto ruleset = catalog.Rulesets[0];
        int person = Random.Shared.Next(ruleset.SeatCount);
        IEnumerable<string> shuffled = Unpaid(catalog).OrderBy(_ => Random.Shared.Next());
        return Start(games, ruleset.Id, BotsOf(Plans(ruleset.SeatCount, person, shuffled)), person);
    }

    /// <summary>
    /// Starts a game of the ruleset with <paramref name="bots"/> in their seats, and sits the player at
    /// <paramref name="person"/>; or has them watch, if that is no seat.
    /// </summary>
    public static async Task<Seating> Start(
        IGameServing games,
        string rulesetId,
        ImmutableList<string?> bots,
        int person
    )
    {
        GameCreatedDto created = await games.Create(new CreateGameRequest(rulesetId, bots), CancellationToken.None);
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
