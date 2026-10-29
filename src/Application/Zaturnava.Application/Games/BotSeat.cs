using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A seat the server plays itself, with the named bot.</summary>
public sealed record BotSeat(string BotId) : SeatController
{
    public const string KindName = "bot";

    public override string Kind => KindName;

    public override string Detail => this.BotId;

    public override string Player => this.BotId;

    public override IDeciding DeciderFrom([Required] IBotLocating bots) => bots.Find(this.BotId);
}
