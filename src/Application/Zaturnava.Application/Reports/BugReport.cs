using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Reports;

/// <summary>
/// A player's report that something at a game may be going wrong (playtest 2026-11-19, T4), kept as structured data:
/// who reported it (<see cref="Reporter"/>), when (<see cref="At"/>), and the game as it stood then, enough to replay
/// it to that point (<see cref="Game"/>, <see cref="RulesetId"/>, <see cref="Version"/>, <see cref="Actions"/>), with
/// who was to decide (<see cref="Deciding"/>) and whether it was over (<see cref="IsOver"/>); the seat the reporter
/// plays there, if any (<see cref="Seat"/>); what their page showed (<see cref="Seen"/>); and what they say
/// (<see cref="Description"/>).
/// </summary>
public sealed record BugReport(
    string Id,
    PlayerId Reporter,
    GameId Game,
    string RulesetId,
    int Version,
    int Actions,
    string Description,
    DateTimeOffset At
)
{
    public SeatId? Deciding { get; init; }

    public bool IsOver { get; init; }

    public SeatId? Seat { get; init; }

    public BugReportSeen Seen { get; init; } = new(null, null, null);
}

/// <summary>
/// What the reporter's page showed: the version of the game it last had (<see cref="ViewVersion"/>), which may be
/// older than the game's own; the cell selected (<see cref="SelectedCell"/>); and the piece whose card was open
/// (<see cref="InspectedPiece"/>); each none, if none.
/// </summary>
public sealed record BugReportSeen(int? ViewVersion, int? SelectedCell, int? InspectedPiece);
