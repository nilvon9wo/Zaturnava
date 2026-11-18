using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>A player that needs no person, and says what it is.</summary>
public interface IBotPlaying : IDeciding
{
    BotProfile Profile { get; }

    /// <summary>
    /// Whether it knows how to play <paramref name="rules"/>'s game by its own lights, not merely as another bot would
    /// (playtest 2026-11-15, D1): a bot that does not is never offered for it. Every game, unless the bot says not.
    /// </summary>
    bool Knows([Required] IRulesApplying rules) => true;
}
