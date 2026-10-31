namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Where the game server is, which the page calls, and sends a person to sign in at.</summary>
internal sealed record ServerAddress(Uri Value);
