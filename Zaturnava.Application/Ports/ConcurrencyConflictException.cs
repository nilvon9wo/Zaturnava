using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

public sealed class ConcurrencyConflictException(GameId id, Exception? innerException = null)
    : GameRequestException($"Game '{id.Value}' changed meanwhile; read it again.", innerException)
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
