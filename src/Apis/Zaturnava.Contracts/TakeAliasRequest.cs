namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The alias a guest, not signed in, would go by for their visit (<see cref="GameRoutes.Aliases"/>): at most
/// <see cref="ClaimSeatRequest.MaximumNicknameLength"/> characters; "Guest", if only spaces. Sent with the token of
/// the alias they hold already, if any, which then lets that one go.
/// </summary>
public sealed record TakeAliasRequest(string? Alias);
