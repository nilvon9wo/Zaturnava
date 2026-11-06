using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A game waiting for a player to decide, and the seat it waits for.</summary>
public sealed record AwaitedGame(GameRecord Record, SeatId Seat);
