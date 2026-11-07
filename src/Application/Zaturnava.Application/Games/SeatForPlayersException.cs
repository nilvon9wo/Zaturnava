using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A guest asked for a seat whoever set the game up left open only for players signed in.</summary>
public sealed class SeatForPlayersException(GameId id, SeatId seat)
    : GameRequestException($"Seat {seat.Value} of game '{id.Value}' is only for players signed in: sign in to take it.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
