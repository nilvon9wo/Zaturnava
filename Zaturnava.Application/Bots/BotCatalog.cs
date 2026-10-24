using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

public sealed class BotCatalog(ImmutableList<IBotPlaying> bots) : IBotLocating
{
    private readonly ImmutableDictionary<string, IBotPlaying> _bots = bots.ToImmutableDictionary(bot => bot.Profile.Id);

    public ImmutableList<BotProfile> Bots =>
        [.. this._bots.Values.Select(bot => bot.Profile).OrderBy(profile => profile.Id, StringComparer.Ordinal)];

    public IDeciding Find([Required] string botId) =>
        this._bots.GetValueOrDefault(botId) ?? throw new UnknownBotException(botId);
}
