namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Say anew who holds the seat <see cref="Token"/> (the seat's token) holds: the player signed in, by their token, as
/// a claim does, or else a guest, by <see cref="Nickname"/>, if one is given, with the <see cref="AliasToken"/> they
/// hold it by, if they do (<see cref="ClaimSeatRequest"/>). For a player who signed in, or chose a nickname, after
/// taking the seat.
/// </summary>
public sealed record IdentifySeatRequest(string Token, string? Nickname = null, string? AliasToken = null);
