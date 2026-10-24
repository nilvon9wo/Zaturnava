using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Bots;

/// <summary>The bots a host can seat. Adding one is registering it, nothing more.</summary>
public interface IBotLocating
{
    ImmutableList<string> BotIds { get; }

    /// <exception cref="UnknownBotException">There is no such bot.</exception>
    IDeciding Find([Required] string botId);
}
