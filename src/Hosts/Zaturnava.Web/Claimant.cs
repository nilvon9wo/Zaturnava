namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// Who takes a seat from this browser: the player signed in, by their token, if one is; else a guest, by the
/// nickname they play by, if they gave one.
/// </summary>
internal sealed record Claimant(string? PlayerToken, string? Nickname);
