using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>How a new game's seat should start: open for a person, or played by the named bot.</summary>
public sealed record SeatPlan(string? BotId)
{
    public static readonly SeatPlan Open = new((string?)null);

    /// <exception cref="UnknownBotException">There is no such bot.</exception>
    public SeatController ToController([Required] IBotLocating bots) =>
        this.BotId is { } botId
            ? BotSeatFor(bots, botId)
            : new OpenSeat();

    private static BotSeat BotSeatFor(IBotLocating bots, string botId)
    {
        _ = bots.Find(botId);
        return new BotSeat(botId);
    }
}
