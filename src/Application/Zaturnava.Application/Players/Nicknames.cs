namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// The nickname a guest, not signed in, gives to be shown by at a table, always as "Nickname (guest)"
/// (<see cref="Shown"/>), so that no guest passes for a player who signed in.
/// </summary>
public static class Nicknames
{
    /// <summary>The longest a nickname may be, in characters; a longer one is cut.</summary>
    public const int MaximumLength = 20;

    /// <summary>
    /// The nickname without control characters or the spaces around it, cut to <see cref="MaximumLength"/>; none,
    /// if nothing is left.
    /// </summary>
    public static string? Of(string? nickname) =>
        Cut(new string([.. (nickname ?? string.Empty).Where(letter => !char.IsControl(letter))]).Trim());

    /// <summary>"Ada (guest)"; or, without a nickname, "guest".</summary>
    public static string Shown(string? nickname) =>
        nickname is null ? PlayerService.Guest : $"{nickname} ({PlayerService.Guest})";

    private static string? Cut(string nickname) =>
        nickname.Length == 0 ? null : nickname[..Math.Min(nickname.Length, MaximumLength)].TrimEnd();
}
