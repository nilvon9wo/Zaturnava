using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>An action came without the token of the seat it is for.</summary>
public sealed class MissingSeatTokenException(GameId id)
    : GameRequestException($"Acting in game '{id.Value}' needs the seat's token, as Authorization: Bearer.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
