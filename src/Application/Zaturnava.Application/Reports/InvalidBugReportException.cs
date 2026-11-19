using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Reports;

public sealed class InvalidBugReportException()
    : GameRequestException($"A bug report may say at most {BugReportService.MaximumLength} characters.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Invalid;
}
