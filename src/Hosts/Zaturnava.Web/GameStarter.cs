using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>How a page starts a game and sits the player at it: as planned, or at once, at random.</summary>
internal static class GameStarter
{
    /// <summary>A seat's level, played by a person rather than a bot of some strength.</summary>
    public const int PersonLevel = -1;

    /// <summary>
    /// A seat's level, left open for an AI of the player's own to take, by MCP or the HTTP API (Proposed); as a
    /// person's, no bot of ours is seated there.
    /// </summary>
    public const int OwnAiLevel = -2;

    private static readonly string[] StrengthNames = ["Easy", "Medium", "Hard"];

    /// <summary>
    /// Whether the strength at <paramref name="level"/> has a bot left for one more seat, beside the strengths
    /// <paramref name="taken"/> by the other seats played by bots.
    /// </summary>
    public static bool HasRoom(ImmutableList<Strength> strengths, IEnumerable<int> taken, int level) =>
        taken.Count(other => other == level) < strengths[level].Bots.Count;

    /// <summary>
    /// The strength nearest <paramref name="wanted"/> (the easier, of two as near) with a bot left beside those
    /// <paramref name="taken"/>; <see cref="PersonLevel"/>, if none has.
    /// </summary>
    public static int LevelWithRoom(ImmutableList<Strength> strengths, IEnumerable<int> taken, int wanted)
    {
        ImmutableList<int> others = [.. taken];
        return Enumerable.Range(0, strengths.Count)
            .Where(level => HasRoom(strengths, others, level))
            .OrderBy(level => Math.Abs(level - wanted))
            .ThenBy(level => level)
            .DefaultIfEmpty(PersonLevel)
            .First();
    }

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
    /// The bots at the ruleset, never one paid for each use, in three strengths - easy, medium and hard - of
    /// as nearly as may be the same number of bots, easiest first; fewer, if there are fewer than three bots.
    /// </summary>
    public static ImmutableList<Strength> Strengths(CatalogDto catalog, string rulesetId)
    {
        ImmutableList<BotDto> easiestFirst =
            [.. catalog.Bots.Where(bot => !bot.IsPaid).OrderBy(bot => bot.Ratings.GetValueOrDefault(rulesetId))];
        int count = easiestFirst.Count;
        int bands = StrengthNames.Length;
        return
        [
            .. StrengthNames
                .Select((name, band) => new Strength(name, rulesetId, BandOf(band)))
                .Where(strength => !strength.Bots.IsEmpty),
        ];

        ImmutableList<BotDto> BandOf(int band) =>
            [.. easiestFirst.Take((band * count / bands)..((band + 1) * count / bands))];
    }

    /// <summary>
    /// A bot for each seat: the one the player chose for it, if they chose one; else, for a seat played at a
    /// strength, one of its strength's, at random, never one already seated or chosen; failing that, the one
    /// rated nearest it of those left; failing that, and for a seat without a strength, none, for a person.
    /// </summary>
    public static ImmutableList<string?> ByStrength(IEnumerable<SeatChoice> seats, ImmutableList<Strength> strengths)
    {
        ImmutableList<SeatChoice> choices = [.. seats];
        ImmutableList<BotDto> all = [.. strengths.SelectMany(strength => strength.Bots)];
        ImmutableList<string?> bots = [.. choices.Select(choice => choice.Bot)];
        foreach (int seat in Enumerable.Range(0, choices.Count))
        {
            SeatChoice choice = choices[seat];
            bots = choice.Bot is null && choice.Strength is { } strength
                ? bots.SetItem(seat, Pick(strength, all, bots))
                : bots;
        }

        return bots;
    }

    /// <summary>
    /// A game of the <paramref name="ruleset"/> at once, as hard as asked (<see cref="QuickBots"/>): the player at a
    /// seat chosen at random, the bots among the others. The seat is the <paramref name="claimant"/>'s.
    /// </summary>
    public static Task<Seating> Quick(
        IGameServing games,
        CatalogDto catalog,
        RulesetDto ruleset,
        Difficulty difficulty,
        Claimant claimant
    )
    {
        int person = Random.Shared.Next(ruleset.SeatCount);
        IEnumerable<string> bots = QuickBots(catalog, ruleset.Id, difficulty, ruleset.SeatCount - 1);
        return Start(games, ruleset.Id, BotsOf(Plans(ruleset.SeatCount, person, bots)), person, null, claimant);
    }

    /// <summary>
    /// Starts a game of the ruleset with <paramref name="bots"/> in their seats, from the start or the
    /// <paramref name="scenario"/>, and sits the <paramref name="claimant"/> at <paramref name="person"/>; or has
    /// them watch, if that is no seat. A game played again after another (<paramref name="continuing"/>) is at its
    /// table.
    /// </summary>
    public static async Task<Seating> Start(
        IGameServing games,
        string rulesetId,
        ImmutableList<string?> bots,
        int person,
        string? scenario,
        Claimant claimant,
        string? continuing = null
    )
    {
        CreateGameRequest request = new(rulesetId, bots) { Scenario = scenario, Continuing = continuing };
        GameCreatedDto created = await games.Create(request, CancellationToken.None);
        return person >= 0
            ? await Claim(games, created.GameId, person, claimant)
            : Seating.Watching(created.GameId);
    }

    /// <summary>
    /// Claims the seat: as the player signed in, by their token, if one is; else as a guest, by their nickname, if
    /// they gave one.
    /// </summary>
    public static async Task<Seating> Claim(IGameServing games, string gameId, int seat, Claimant claimant)
    {
        SeatClaimedDto claimed =
            await games.Claim(
                gameId,
                seat,
                claimant.PlayerToken,
                new ClaimSeatRequest(claimant.Nickname),
                CancellationToken.None
            );
        return new Seating(gameId, new SeatClaim(seat, claimed.Token));
    }

    /// <summary>
    /// Goes back to a seat held: first saying anew who holds it, the <paramref name="claimant"/> as they are now (the
    /// player signed in since, say, or the nickname chosen since). Should that fail (the player's token no longer
    /// standing for anyone, say), the seat is still held, as it was.
    /// </summary>
    public static async Task<Seating> Resume(IGameServing games, string gameId, SeatClaim claim, Claimant claimant)
    {
        IdentifySeatRequest request = new(claim.Token, claimant.Nickname);
        _ = await Requests.Attempt(
            () => games.Identify(gameId, claim.Seat, claimant.PlayerToken, request, CancellationToken.None)
        );
        return new Seating(gameId, claim);
    }

    private static string? Pick(Strength strength, ImmutableList<BotDto> all, ImmutableList<string?> seated) =>
        strength.Bots
            .OrderBy(_ => Random.Shared.Next())
            .Concat(all.OrderBy(strength.DistanceTo))
            .Select(bot => bot.Id)
            .FirstOrDefault(bot => !seated.Contains(bot));
}
