namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A token for an AI to play for a player (<see cref="GameRoutes.AiToken"/>), shown as a player's token is: the seats
/// it claims are an AI's, and it may not change the player's account. Signing out with it lets it go.
/// </summary>
public sealed record AiTokenDto(string Token);
