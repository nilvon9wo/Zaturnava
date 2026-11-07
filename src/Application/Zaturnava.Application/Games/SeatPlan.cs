using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// How a new game's seat should start: open for a person (only a player signed in, if
/// <see cref="IsForPlayers"/>), or played by the named bot.
/// </summary>
public sealed record SeatPlan(string? BotId)
{
    public static readonly SeatPlan Open = new((string?)null);

    /// <summary>Whether, left open, the seat is only for a player signed in: no guest may claim it.</summary>
    public bool IsForPlayers { get; init; }

    /// <exception cref="UnknownBotException">There is no such bot.</exception>
    public SeatController ToController([Required] IBotLocating bots) =>
        this.BotId is { } botId
            ? BotSeatFor(bots, botId)
            : new OpenSeat { IsForPlayers = this.IsForPlayers };

    private static BotSeat BotSeatFor(IBotLocating bots, string botId)
    {
        _ = bots.Find(botId);
        return new BotSeat(botId);
    }
}
