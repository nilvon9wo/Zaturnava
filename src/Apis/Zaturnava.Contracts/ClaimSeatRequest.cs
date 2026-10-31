namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Claim a seat; a guest, not signed in, may give a <see cref="Nickname"/> to be shown by, as "Nickname (guest)".
/// The body may be left out altogether.
/// </summary>
public sealed record ClaimSeatRequest(string? Nickname = null)
{
    /// <summary>The longest a nickname may be, in characters; a longer one is cut.</summary>
    public const int MaximumNicknameLength = 20;
}
