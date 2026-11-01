using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>An AI's token (<see cref="PlayerService.AiToken"/>) asked to change the player's account.</summary>
public sealed class AiTokenRefusedException()
    : GameRequestException("An AI's token may play and talk for its player, but not change their account.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
