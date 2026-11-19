namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

/// <summary>A player's bug report, a column a fact (<see cref="Application.Reports.BugReport"/>).</summary>
/// <remarks>
/// Its strings are unset (null) until assigned, not defaulted, as <see cref="ActionEntity"/>'s are. The model in
/// <see cref="GameDbContext"/> keeps those columns required.
/// </remarks>
public sealed class BugReportEntity
{
    public string Id { get; set; } = null!;

    public string ReporterId { get; set; } = null!;

    public string GameId { get; set; } = null!;

    public string RulesetId { get; set; } = null!;

    public int Version { get; set; }

    public int Actions { get; set; }

    public int? Deciding { get; set; }

    public bool IsOver { get; set; }

    public int? Seat { get; set; }

    public int? ViewVersion { get; set; }

    public int? SelectedCell { get; set; }

    public int? InspectedPiece { get; set; }

    public string Description { get; set; } = null!;

    /// <summary>When it was reported, in UTC, as SQLite can order only that.</summary>
    public DateTime At { get; set; }
}
