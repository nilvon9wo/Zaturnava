using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How the player last left a list (the lobby's games, a leaderboard), to show it so again (Stated, 2026-11-19: "the
/// UI should remember any filtering on the games list -- and sorting"): its choices, each by name, and the column it
/// is sorted by, either way. A choice left as it starts is not kept.
/// </summary>
internal sealed record ListView(ImmutableDictionary<string, string> Choices, string Sort, bool IsDescending)
{
    private const string SortKey = "sort";
    private const string DescendingKey = "desc";

    /// <summary>
    /// As the browser keeps it: "desc=1&amp;players=Bob%2C%20open&amp;sort=moves&amp;state=waiting", each value
    /// escaped, so that a comma or an ampersand typed into a filter is kept as it was typed.
    /// </summary>
    public string Text =>
        string.Join(
            '&',
            this.Choices
                .SetItem(SortKey, this.Sort)
                .SetItem(DescendingKey, this.IsDescending ? "1" : string.Empty)
                .Where(choice => choice.Value.Length > 0)
                .OrderBy(choice => choice.Key, StringComparer.Ordinal)
                .Select(choice => $"{choice.Key}={Uri.EscapeDataString(choice.Value)}")
        );

    /// <summary>A choice kept, by its name; none, if it was not.</summary>
    public string? this[string name] => this.Choices.GetValueOrDefault(name);

    /// <summary>The view <paramref name="text"/> keeps; none, if it keeps none.</summary>
    public static ListView? Parse(string? text)
    {
        ImmutableDictionary<string, string> all = (text ?? string.Empty)
            .Split('&')
            .Select(choice => choice.Split('='))
            .Where(choice => choice is [{ Length: > 0 }, _])
            .GroupBy(choice => choice[0], StringComparer.Ordinal)
            .ToImmutableDictionary(
                choice => choice.Key,
                choice => Uri.UnescapeDataString(choice.First()[1]),
                StringComparer.Ordinal
            );
        return all.IsEmpty
            ? null
            : new ListView(
                all.Remove(SortKey).Remove(DescendingKey),
                all.GetValueOrDefault(SortKey, string.Empty),
                all.ContainsKey(DescendingKey)
            );
    }
}
