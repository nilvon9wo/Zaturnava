using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>
/// The bots, and how each is rated at each ruleset (<paramref name="ratings"/>: by the ruleset's id, then
/// the bot's), which their profiles then say.
/// </summary>
public sealed class BotCatalog(
    ImmutableList<IBotPlaying> bots,
    ImmutableDictionary<string, ImmutableDictionary<string, int>> ratings
) : IBotLocating
{
    private readonly ImmutableDictionary<string, IBotPlaying> _bots = bots.ToImmutableDictionary(bot => bot.Profile.Id);
    private readonly ImmutableDictionary<string, ImmutableDictionary<string, int>> _ratings = ratings;

    /// <summary>The bots, rated at nothing.</summary>
    public BotCatalog(ImmutableList<IBotPlaying> bots)
        : this(bots, [])
    {
    }

    public ImmutableList<BotProfile> Bots =>
        [
            .. this._bots.Values
                .Select(bot => this.Rated(bot.Profile))
                .OrderBy(profile => profile.Id, StringComparer.Ordinal),
        ];

    public IDeciding Find([Required] string botId) =>
        this._bots.GetValueOrDefault(botId) ?? throw new UnknownBotException(botId);

    private BotProfile Rated(BotProfile profile) =>
        profile with
        {
            Ratings = this._ratings
                .Where(ruleset => ruleset.Value.ContainsKey(profile.Id))
                .ToImmutableDictionary(ruleset => ruleset.Key, ruleset => ruleset.Value[profile.Id]),
        };
}
