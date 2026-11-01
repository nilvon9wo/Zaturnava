using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What a page calls whoever plays a seat: the bot, or the person, as the server names them (a player's name,
/// "Ada (guest)", "Hal (AI guest)"...), before the seat's own name, so that the players are known by name wherever
/// the page speaks of them, and the seat's colour still says which army is theirs.
/// </summary>
internal static class SeatLabels
{
    /// <summary>The bot, or the person, playing the seat; "open", if no one does yet.</summary>
    public static string Person(SeatDto seat) => seat.Bot ?? seat.Player ?? seat.Controller;

    /// <summary>"Ada (Black)", "Argus (Yellow)"; "Red", for a seat no one has taken yet.</summary>
    public static string Of(SeatDto seat) =>
        seat.Bot is null && seat.Player is null ? seat.Name : $"{Person(seat)} ({seat.Name})";

    /// <summary>The player's own seat: "Ada · you", "Ada (guest) · you"; "You", for a guest with no nickname.</summary>
    public static string Mine(SeatDto seat) =>
        seat.Player is null or GameSummaryDto.Guest ? "You" : $"{seat.Player} · you";
}
