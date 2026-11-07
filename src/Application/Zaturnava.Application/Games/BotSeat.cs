using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A seat the server plays itself, with the named bot; only a player signed in may take it over, if
/// <see cref="SeatController.IsForPlayers"/>.
/// </summary>
public sealed record BotSeat(string BotId) : SeatController
{
    public const string KindName = "bot";

    /// <summary>What marks, in <see cref="Detail"/>, before the bot's id, a seat only for players signed in.</summary>
    private const char ForPlayersMark = '+';

    public override string Kind => KindName;

    public override string Detail => this.IsForPlayers ? $"{ForPlayersMark}{this.BotId}" : this.BotId;

    public override string Player => this.BotId;

    public override IDeciding DeciderFrom([Required] IBotLocating bots) => bots.Find(this.BotId);

    /// <summary>The seat as <see cref="Detail"/> stores it.</summary>
    public static BotSeat From([Required] string detail) =>
        detail is [ForPlayersMark, .. { } bot] ? new BotSeat(bot) { IsForPlayers = true } : new BotSeat(detail);
}
