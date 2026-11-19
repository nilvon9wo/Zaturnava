using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Reports;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>The bug reports players have sent, in a relational database, a column a fact.</summary>
public sealed class EfBugReportStore(GameDbContext context) : IBugReportPersisting
{
    private readonly GameDbContext _context = context;

    public async Task Add([Required] BugReport report, CancellationToken cancellationToken)
    {
        _ = this._context.BugReports.Add(ToEntity(report));
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImmutableList<BugReport>> All(CancellationToken cancellationToken)
    {
        List<BugReportEntity> entities = await this._context.BugReports
            .AsNoTracking()
            .OrderBy(report => report.At)
            .ThenBy(report => report.Id)
            .ToListAsync(cancellationToken);
        return [.. entities.Select(ToReport)];
    }

    private static BugReportEntity ToEntity(BugReport report) =>
        new()
        {
            Id = report.Id,
            ReporterId = report.Reporter.Value,
            GameId = report.Game.Value,
            RulesetId = report.RulesetId,
            Version = report.Version,
            Actions = report.Actions,
            Deciding = report.Deciding?.Value,
            IsOver = report.IsOver,
            Seat = report.Seat?.Value,
            ViewVersion = report.Seen.ViewVersion,
            SelectedCell = report.Seen.SelectedCell,
            InspectedPiece = report.Seen.InspectedPiece,
            Description = report.Description,
            At = report.At.UtcDateTime,
        };

    private static BugReport ToReport(BugReportEntity entity) =>
        new(
            entity.Id,
            new PlayerId(entity.ReporterId),
            new GameId(entity.GameId),
            entity.RulesetId,
            entity.Version,
            entity.Actions,
            entity.Description,
            new DateTimeOffset(DateTime.SpecifyKind(entity.At, DateTimeKind.Utc))
        )
        {
            Deciding = SeatOf(entity.Deciding),
            IsOver = entity.IsOver,
            Seat = SeatOf(entity.Seat),
            Seen = new BugReportSeen(entity.ViewVersion, entity.SelectedCell, entity.InspectedPiece),
        };

    private static SeatId? SeatOf(int? seat) => seat is { } value ? new SeatId(value) : null;
}
