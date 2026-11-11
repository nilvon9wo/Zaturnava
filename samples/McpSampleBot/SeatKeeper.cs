using System.Globalization;

namespace McpSampleBot;

/// <summary>
/// The tokens of the seats this bot has claimed, kept on this computer (in the user's local application data, never
/// beside the code), so that a bot stopped mid-game and run again to join the same seat plays on by the token it was
/// given, rather than asking for a seat no longer open. Each is let go of once its game is over.
/// </summary>
internal static class SeatKeeper
{
    /// <summary>The token kept for the seat, if any.</summary>
    public static string? Kept(string gameId, int seat) =>
        PathOf(gameId, seat) is { } path && File.Exists(path)
            ? File.ReadAllText(path).Trim()
            : null;

    /// <summary>Keeps the seat's token, for the bot to play on by if run again.</summary>
    public static void Keep(string gameId, int seat, string token)
    {
        if (PathOf(gameId, seat) is { } path)
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, token);
        }
    }

    /// <summary>Lets go of the seat's token: its game is over.</summary>
    public static void Forget(string gameId, int seat)
    {
        if (PathOf(gameId, seat) is { } path)
        {
            File.Delete(path);
        }
    }

    /// <summary>Where the seat's token is kept; nowhere, for a game id that is not letters and digits only.</summary>
    private static string? PathOf(string gameId, int seat) =>
        gameId.Length > 0 && gameId.All(char.IsAsciiLetterOrDigit)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Zaturnava",
                "seats",
                $"{gameId}-{seat.ToString(CultureInfo.InvariantCulture)}.token"
            )
            : null;
}
