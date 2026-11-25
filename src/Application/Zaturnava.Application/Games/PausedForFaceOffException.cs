using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>The game is paused for a player to see the leader face-off they may call; only they may act.</summary>
public sealed class PausedForFaceOffException(GameId id, FaceOffPause pause)
    : GameRequestException(
        string.Create(
            CultureInfo.InvariantCulture,
            $"Game '{id.Value}' is paused {Math.Ceiling(pause.Left.TotalSeconds)} seconds more for seat "
                + $"{pause.Seat.Value} to see the leader face-off it may call."
        )
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
