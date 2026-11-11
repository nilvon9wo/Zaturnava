namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Claim a seat; a guest, not signed in, may give a <see cref="Nickname"/> to be shown by, as "Nickname (guest)".
/// <see cref="IsAi"/> says that an AI, or another program, will play the seat, rather than a person: the seat is then
/// shown so ("Ada (AI)", "Nickname (AI guest)"), and the players may hide what it says. An AI keeps its nickname even
/// when it plays for a player signed in ("Sage (Ada's AI)"), so that one player's AIs may be told apart. The body may
/// be left out altogether. A guest's nickname that a player goes by, or that another guest holds as their alias
/// (<see cref="GuestAliasDto"/>), is refused: the guest who holds it sends its <see cref="AliasToken"/>.
/// </summary>
public sealed record ClaimSeatRequest(string? Nickname = null, bool IsAi = false, string? AliasToken = null)
{
    /// <summary>The longest a nickname may be, in characters; a longer one is cut.</summary>
    public const int MaximumNicknameLength = 20;
}
