namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// How a player is known to a sign-in provider: the provider's name (as the server's configuration calls it) and the
/// player's id there, which never changes, unlike their name.
/// </summary>
public readonly record struct Login(string Provider, string Key);
