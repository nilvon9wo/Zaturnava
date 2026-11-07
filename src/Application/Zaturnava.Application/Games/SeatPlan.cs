using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// How a new game's seat should start: open for a person, or played by the named bot; only for a player signed in to
/// claim or take over, if <see cref="IsForPlayers"/>.
/// </summary>
public sealed record SeatPlan(string? BotId)
{
    public static readonly SeatPlan Open = new((string?)null);

    /// <summary>Whether the seat is only for a player signed in: no guest may claim it or take it over.</summary>
    public bool IsForPlayers { get; init; }

    /// <exception cref="UnknownBotException">There is no such bot.</exception>
    public SeatController ToController([Required] IBotLocating bots) =>
        this.BotId is { } botId
            ? BotSeatFor(bots, botId) with { IsForPlayers = this.IsForPlayers }
            : new OpenSeat { IsForPlayers = this.IsForPlayers };

    private static BotSeat BotSeatFor(IBotLocating bots, string botId)
    {
        _ = bots.Find(botId);
        return new BotSeat(botId);
    }
}
