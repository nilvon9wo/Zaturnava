namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The alias a guest holds for their visit (<see cref="GuestAliases"/>), and the token they hold it by.
/// </summary>
public sealed record GuestAlias(string Alias, string Token);
