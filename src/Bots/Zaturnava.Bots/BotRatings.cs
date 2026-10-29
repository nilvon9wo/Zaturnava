using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How strongly each bot plays each ruleset: its Elo rating, from games between every pair of them, as
/// <c>scripts/rate-bots.py</c> fits it, with Random at 1000. Kept in this assembly, one JSON file for each
/// ruleset's id, by bot.
/// </summary>
public static class BotRatings
{
    private const string Folder = "Ratings/";

    /// <summary>The ratings at each ruleset, by its id, then by the bot's; a ruleset not there is unrated.</summary>
    public static ImmutableDictionary<string, ImmutableDictionary<string, int>> Rated { get; } =
        RatedIn(typeof(BotRatings).Assembly);

    private static ImmutableDictionary<string, ImmutableDictionary<string, int>> RatedIn(Assembly assembly) =>
        assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(Folder, StringComparison.Ordinal))
            .ToImmutableDictionary(
                name => Path.GetFileNameWithoutExtension(name[Folder.Length..]),
                name => Read(assembly, name)
            );

    private static ImmutableDictionary<string, int> Read(Assembly assembly, string name)
    {
        using Stream stream = assembly.GetManifestResourceStream(name)!; // listed by the assembly itself
        return JsonSerializer.Deserialize<ImmutableDictionary<string, int>>(stream)!; // ours, so whole
    }
}
