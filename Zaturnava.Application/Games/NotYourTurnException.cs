using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class NotYourTurnException(GameId id, SeatId seat)
    : GameRequestException($"Seat {seat.Value} of game '{id.Value}' is not yours to play now.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
