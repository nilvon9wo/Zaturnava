using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A guest asked for a seat whoever set the game up kept only for players signed in: to claim it, or to take it over
/// from a bot.
/// </summary>
public sealed class SeatForPlayersException(GameId id, SeatId seat)
    : GameRequestException($"Seat {seat.Value} of game '{id.Value}' is only for players signed in: sign in to take it.")
{
    public override GameRequestFailure Failure => GameRequestFailure.Forbidden;
}
