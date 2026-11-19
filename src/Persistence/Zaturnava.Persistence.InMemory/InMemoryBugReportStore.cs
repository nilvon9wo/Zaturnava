using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Reports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryBugReportStore : IBugReportPersisting
{
    private ImmutableList<BugReport> _reports = [];

    public Task Add([Required] BugReport report, CancellationToken cancellationToken)
    {
        _ = ImmutableInterlocked.Update(ref this._reports, reports => reports.Add(report));
        return Task.CompletedTask;
    }

    public Task<ImmutableList<BugReport>> All(CancellationToken cancellationToken) => Task.FromResult(this._reports);
}
