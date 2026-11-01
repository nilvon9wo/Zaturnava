namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Say anew who holds the seat <see cref="Token"/> (the seat's token) holds: the player signed in, by their token, as
/// a claim does, or else a guest, by <see cref="Nickname"/>, if one is given. For a player who signed in, or chose a
/// nickname, after taking the seat.
/// </summary>
public sealed record IdentifySeatRequest(string Token, string? Nickname = null);
