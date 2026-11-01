using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class NotYourSeatException(GameId id, SeatId seat)
    : GameRequestException($"The token does not hold seat {seat.Value} of game '{id.Value}'.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
