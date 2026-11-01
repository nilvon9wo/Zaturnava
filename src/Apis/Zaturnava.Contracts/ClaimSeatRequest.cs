namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Claim a seat; a guest, not signed in, may give a <see cref="Nickname"/> to be shown by, as "Nickname (guest)".
/// <see cref="IsAi"/> says that an AI, or another program, will play the seat, rather than a person: the seat is then
/// shown so ("Ada (AI)", "Nickname (AI guest)"), and the players may hide what it says. The body may be left out
/// altogether.
/// </summary>
public sealed record ClaimSeatRequest(string? Nickname = null, bool IsAi = false)
{
    /// <summary>The longest a nickname may be, in characters; a longer one is cut.</summary>
    public const int MaximumNicknameLength = 20;
}
