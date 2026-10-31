namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A person just signed in: their name, and the token that is now their credential, shown once and sent back as
/// <c>Authorization: Bearer {token}</c> (see <see cref="GameRoutes.Me"/>).
/// </summary>
public sealed record SignedInDto(string Token, string Name);
