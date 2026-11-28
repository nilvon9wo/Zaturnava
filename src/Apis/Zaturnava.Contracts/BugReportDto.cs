namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A bug report kept (<see cref="GameRoutes.BugReport"/>), read back by its id, for whoever looks into it: the game
/// and the point it had reached (its <see cref="Version"/> and how many <see cref="Actions"/> it held), what the
/// reporter said, and when. Who sent it is not told.
/// </summary>
public sealed record BugReportDto(
    string Id,
    string GameId,
    string RulesetId,
    int Version,
    int Actions,
    string Description,
    DateTimeOffset At
)
{
    /// <summary>The seat that was to decide, if any.</summary>
    public int? Deciding { get; init; }

    public bool IsOver { get; init; }

    /// <summary>The seat the reporter plays at the game, if any.</summary>
    public int? Seat { get; init; }

    /// <summary>The version of the game the reporter's page last had, if it said.</summary>
    public int? ViewVersion { get; init; }

    /// <summary>The cell selected on the reporter's page, if any.</summary>
    public int? SelectedCell { get; init; }

    /// <summary>The piece whose card was open on the reporter's page, if any.</summary>
    public int? InspectedPiece { get; init; }
}
