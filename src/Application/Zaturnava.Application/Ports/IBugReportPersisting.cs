using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Reports;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>The bug reports players have sent (<see cref="BugReport"/>), kept apart from the games.</summary>
public interface IBugReportPersisting
{
    Task Add([Required] BugReport report, CancellationToken cancellationToken);

    /// <summary>Every report kept, the earliest first.</summary>
    Task<ImmutableList<BugReport>> All(CancellationToken cancellationToken);
}
