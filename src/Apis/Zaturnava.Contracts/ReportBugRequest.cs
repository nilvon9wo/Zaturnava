namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A signed-in player's report that something at a game may be going wrong (<see cref="GameRoutes.BugReports"/>): what
/// they say (<paramref name="Description"/>), at most <see cref="MaximumLength"/> characters, and what their page
/// showed: the game's version it last had (<see cref="ViewVersion"/>), the cell selected (<see cref="SelectedCell"/>)
/// and the piece whose card was open (<see cref="InspectedPiece"/>), each none if none. The server keeps the game as it
/// stands beside them.
/// </summary>
public sealed record ReportBugRequest(string Description)
{
    public const int MaximumLength = 2000;

    public int? ViewVersion { get; init; }

    public int? SelectedCell { get; init; }

    public int? InspectedPiece { get; init; }
}
