namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A request the application refused, and what kind of refusal it was, for any host to report.</summary>
public abstract class GameRequestException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract GameRequestFailure Failure { get; }
}
