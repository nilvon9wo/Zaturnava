using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// Who a lobby's list of games is filtered by (Proposed), as <see cref="GameListQuery.Players"/> says: names,
/// separated by commas, each of which must match someone at a game, in any seat. A seat's player is matched by what
/// it is (<c>open</c>, <c>person</c>, or a bot's id), a person by what the lobby calls them, and a bot by its name;
/// a name with or without what follows it in brackets. <c>*</c> stands for any letters, and case does not count.
/// </summary>
internal static class PlayerPatterns
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <summary>The names asked for, trimmed; none, if none is.</summary>
    public static ImmutableList<string> Of(string? players) =>
    [
        .. (players ?? string.Empty).Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        ),
    ];

    /// <summary>
    /// Whether each of <paramref name="patterns"/> matches someone at <paramref name="game"/>, each person called
    /// what <paramref name="names"/> call them, each bot what <paramref name="bots"/> do.
    /// </summary>
    public static bool AreAllAt(
        ImmutableList<string> patterns,
        GameListing game,
        ImmutableDictionary<HumanSeat, string> names,
        ImmutableList<BotProfile> bots
    ) =>
        patterns.All(pattern =>
            game.Controllers.Any(seat => NamesOf(seat, names, bots).Any(name => Matches(pattern, name)))
        );

    /// <summary>Whether <paramref name="name"/> is <paramref name="pattern"/>, each <c>*</c> any letters.</summary>
    public static bool Matches(string pattern, string name) =>
        Regex.IsMatch(
            name,
            "^" + Regex.Escape(pattern).Replace(@"\*", ".*", StringComparison.Ordinal) + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline,
            Timeout
        );

    /// <summary>What a seat's player may be called: "random", "Random"; "person", "Bob (guest)", "Bob".</summary>
    private static IEnumerable<string> NamesOf(
        SeatController seat,
        ImmutableDictionary<HumanSeat, string> names,
        ImmutableList<BotProfile> bots
    )
    {
        IEnumerable<string> shown = seat switch
        {
            HumanSeat person => [names.GetValueOrDefault(person, GameSummaryDto.Guest)],
            BotSeat bot => bots.Where(profile => profile.Id == bot.BotId).Select(profile => profile.Name),
            _ => [],
        };
        return shown.SelectMany(name => new[] { name, Unbracketed(name) }).Prepend(seat.Player);
    }

    /// <summary>"Bob" of "Bob (guest)"; a name with nothing in brackets as it is.</summary>
    private static string Unbracketed(string name)
    {
        int bracket = name.IndexOf(" (", StringComparison.Ordinal);
        return bracket < 0 ? name : name[..bracket];
    }
}
