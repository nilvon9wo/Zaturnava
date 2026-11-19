using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Reports;

/// <summary>
/// Bug reports (playtest 2026-11-19, T4): a player signed in says something may be going wrong at a game, and the
/// game as it stands is kept beside what they say, for whoever looks into it.
/// </summary>
public sealed class BugReportService(GameService games, IBugReportPersisting store, DecisionClock clock)
{
    /// <summary>The most a report may say, in characters (as <c>ReportBugRequest.MaximumLength</c> says).</summary>
    public const int MaximumLength = 2000;

    private readonly GameService _games = games;
    private readonly IBugReportPersisting _store = store;
    private readonly DecisionClock _clock = clock;

    /// <summary>The report, kept: what the player says, trimmed of spaces, and the game as it stands.</summary>
    /// <exception cref="GameNotFoundException">There is no such game.</exception>
    /// <exception cref="InvalidBugReportException">The report says too much.</exception>
    public async Task<BugReport> Report(
        [Required] GameId id,
        PlayerId reporter,
        [Required] string description,
        [Required] BugReportSeen seen,
        CancellationToken cancellationToken
    )
    {
        string said = description.Trim();
        if (said.Length > MaximumLength)
        {
            throw new InvalidBugReportException();
        }

        GameSnapshot snapshot = await this._games.Get(id, cancellationToken);
        GameRecord record = snapshot.Record;
        int seat = record.Controllers.FindIndex(controller =>
            controller is HumanSeat human && reporter.Equals(human.Claimant)
        );
        BugReport report = new(
            Guid.NewGuid().ToString("N"),
            reporter,
            id,
            record.RulesetId,
            record.Version,
            record.Actions.Count,
            said,
            this._clock.Now
        )
        {
            Deciding = snapshot.State.DecidingSeat,
            IsOver = snapshot.State.Outcome is not null,
            Seat = seat < 0 ? null : new SeatId(seat),
            Seen = seen,
        };
        await this._store.Add(report, cancellationToken);
        return report;
    }
}
