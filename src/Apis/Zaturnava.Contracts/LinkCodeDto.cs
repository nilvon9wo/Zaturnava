namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A one-time code to link another service to the player who asked for it: given to <see cref="GameRoutes.SignIn"/>,
/// it adds that service's account to them, rather than signing in someone new. It lasts five minutes.
/// </summary>
public sealed record LinkCodeDto(string Code);
