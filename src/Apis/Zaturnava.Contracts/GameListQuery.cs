using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Which games to list, and which page (Proposed): of one ruleset; with open seats or without; with no seat played
/// by a bot (<see cref="PeopleOnly"/>); over or not. A filter left null does not filter. Pages count from 1; a page
/// holds at most 100 games, 20 unless asked otherwise.
/// </summary>
public sealed record GameListQuery(
    string? Ruleset = null,
    bool? Open = null,
    bool? PeopleOnly = null,
    bool? Over = null,
    int? Page = null,
    int? PageSize = null
)
{
    /// <summary>The query as an address's query string, from its "?"; empty, if it asks nothing.</summary>
    public string QueryString()
    {
        IEnumerable<string> parts = new (string Name, string? Value)[]
            {
                ("ruleset", this.Ruleset is { } ruleset ? Uri.EscapeDataString(ruleset) : null),
                ("open", Flag(this.Open)),
                ("peopleOnly", Flag(this.PeopleOnly)),
                ("over", Flag(this.Over)),
                ("page", Number(this.Page)),
                ("pageSize", Number(this.PageSize)),
            }
            .Where(part => part.Value is not null)
            .Select(part => $"{part.Name}={part.Value}");
        string query = string.Join('&', parts);
        return query.Length == 0 ? string.Empty : "?" + query;
    }

    private static string? Flag(bool? flag) => flag?.ToString().ToLowerInvariant();

    private static string? Number(int? number) => number?.ToString(CultureInfo.InvariantCulture);
}
