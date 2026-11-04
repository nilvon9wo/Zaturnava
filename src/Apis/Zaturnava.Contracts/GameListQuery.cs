using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Which games to list, and which page (Proposed): of one game, at any size (<see cref="Game"/>, as
/// <see cref="RulesetDto.Game"/>), or of one ruleset, which is one game at one size; with open seats or without; with
/// no seat played by a bot (<see cref="PeopleOnly"/>); over or not; of preview games or not
/// (<see cref="RulesetDto.IsPreview"/>); with everyone named in <see cref="Players"/> at it. A filter left null does
/// not filter. Pages count from 1; a page holds at most 100 games, 20 unless asked otherwise.
/// </summary>
/// <param name="Players">
/// Who must be at the game, in any seats, separated by commas: "Bob, Joe, open" lists the games Bob and Joe both play,
/// with a seat open. Each is a person's name, a bot's name or id, <c>open</c> for an open seat, or <c>person</c> for
/// anyone's; <c>*</c> stands for any letters ("Bo*"), and case does not count. A name matches with or without what
/// follows it in brackets: "Ada" matches "Ada (guest)".
/// </param>
public sealed record GameListQuery(
    string? Ruleset = null,
    bool? Open = null,
    bool? PeopleOnly = null,
    bool? Over = null,
    int? Page = null,
    int? PageSize = null,
    string? Game = null,
    bool? Preview = null,
    string? Players = null
)
{
    /// <summary>The query as an address's query string, from its "?"; empty, if it asks nothing.</summary>
    public string QueryString()
    {
        IEnumerable<string> parts = new (string Name, string? Value)[]
            {
                ("game", Escaped(this.Game)),
                ("ruleset", Escaped(this.Ruleset)),
                ("open", Flag(this.Open)),
                ("peopleOnly", Flag(this.PeopleOnly)),
                ("over", Flag(this.Over)),
                ("preview", Flag(this.Preview)),
                ("players", Escaped(this.Players)),
                ("page", Number(this.Page)),
                ("pageSize", Number(this.PageSize)),
            }
            .Where(part => part.Value is not null)
            .Select(part => $"{part.Name}={part.Value}");
        string query = string.Join('&', parts);
        return query.Length == 0 ? string.Empty : "?" + query;
    }

    private static string? Escaped(string? text) => text is null ? null : Uri.EscapeDataString(text);

    private static string? Flag(bool? flag) => flag?.ToString().ToLowerInvariant();

    private static string? Number(int? number) => number?.ToString(CultureInfo.InvariantCulture);
}
