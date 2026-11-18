using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>The bots a host can seat. Adding one is registering it, nothing more.</summary>
public interface IBotLocating
{
    /// <summary>Every bot, in order of id.</summary>
    ImmutableList<BotProfile> Bots { get; }

    /// <exception cref="UnknownBotException">There is no such bot.</exception>
    IDeciding Find([Required] string botId);

    /// <summary>
    /// Whether the bot of <paramref name="botId"/> knows how to play <paramref name="rules"/>
    /// (<see cref="IBotPlaying.Knows"/>); every bot, every ruleset, unless the locator says otherwise.
    /// </summary>
    bool Knows([Required] string botId, [Required] IRulesApplying rules) => true;
}
