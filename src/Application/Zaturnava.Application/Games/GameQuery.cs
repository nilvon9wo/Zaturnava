namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// Which games a lobby lists, and which page of them (Proposed): of one ruleset, if <see cref="RulesetId"/> is
/// given; with or without open seats; with no seat played by a bot (<see cref="PeopleOnly"/>); over or not. A filter
/// left null leaves the games unfiltered by it. Pages count from 1, newest change first.
/// </summary>
public sealed record GameQuery(
    string? RulesetId = null,
    bool? HasOpenSeats = null,
    bool? PeopleOnly = null,
    bool? IsOver = null,
    int Page = 1,
    int PageSize = GameQuery.DefaultPageSize
)
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    /// <summary>The page asked for, kept to 1 or more, and its size to 1 to <see cref="MaximumPageSize"/>.</summary>
    public GameQuery Bounded() =>
        this with { Page = Math.Max(1, this.Page), PageSize = Math.Clamp(this.PageSize, 1, MaximumPageSize) };

    /// <summary>How many games come before the page.</summary>
    public int Skipped => (this.Page - 1) * this.PageSize;
}
