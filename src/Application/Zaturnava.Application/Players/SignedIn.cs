namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>A player just signed in, and the token now their credential: shown once, kept only hashed.</summary>
public sealed record SignedIn(Player Player, string Token);
