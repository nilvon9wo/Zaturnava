using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A player asked to take a seat over from a bot (<see cref="GameService.TakeOver"/>) that no bot plays, at a game
/// over, or at which they hold a seat already.
/// </summary>
public sealed class SeatNotTakenOverException(GameId id, SeatId seat)
    : GameRequestException(
        $"Seat {seat.Value} of game '{id.Value}' cannot be taken over: only a bot's seat may be, in a game not over, "
            + "by a player holding no seat at it."
    )
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
