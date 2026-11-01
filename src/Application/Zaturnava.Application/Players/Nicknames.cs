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

    /// <summary>What marks a seat an AI plays.</summary>
    public const string Ai = "AI";

    /// <summary>"Ada (guest)"; or, without a nickname, "guest". For an AI: "Ada (AI guest)", "AI guest".</summary>
    public static string Shown(string? nickname, bool isAi = false)
    {
        string guest = isAi ? $"{Ai} {PlayerService.Guest}" : PlayerService.Guest;
        return nickname is null ? guest : $"{nickname} ({guest})";
    }

    /// <summary>A signed-in player's name; for an AI playing on their behalf, "Ada (AI)".</summary>
    public static string Labelled(string name, bool isAi) => isAi ? $"{name} ({Ai})" : name;

    private static string? Cut(string nickname) =>
        nickname.Length == 0 ? null : nickname[..Math.Min(nickname.Length, MaximumLength)].TrimEnd();
}
