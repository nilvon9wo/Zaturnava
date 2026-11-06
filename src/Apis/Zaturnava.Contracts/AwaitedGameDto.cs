namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A game waiting for the player signed in to decide (<see cref="GameRoutes.Awaiting"/>), as a lobby lists it, and the
/// seat it waits for: theirs, which they may go back to, or take back from another device
/// (<see cref="GameRoutes.TakeBack"/>).
/// </summary>
public sealed record AwaitedGameDto(GameSummaryDto Game, int Seat);
