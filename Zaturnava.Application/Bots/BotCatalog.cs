using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

public sealed class BotCatalog(ImmutableDictionary<string, IDeciding> bots) : IBotLocating
{
    private readonly ImmutableDictionary<string, IDeciding> _bots = bots;

    public ImmutableList<string> BotIds => [.. this._bots.Keys.Order()];

    public IDeciding Find([Required] string botId) =>
        this._bots.GetValueOrDefault(botId) ?? throw new UnknownBotException(botId);
}
