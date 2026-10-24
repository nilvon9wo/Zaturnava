using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

public sealed class SeatUnavailableException(GameId id, SeatId seat)
    : GameRequestException($"Seat {seat.Value} of game '{id.Value}' is not open.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Conflict;
}
