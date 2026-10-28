namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The token that now holds a claimed seat: shown once, and sent back with every action for that
/// seat (see <see cref="GameRoutes.TokenScheme"/>).
/// </summary>
public sealed record SeatClaimedDto(string Token);
