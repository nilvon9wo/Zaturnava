using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>
/// A predictable bot: always the first legal action; its id, <paramref name="id"/>; knowing only the rulesets of
/// <paramref name="known"/>, if any are given, else every one.
/// </summary>
internal sealed class FirstChoiceBot(string id = FirstChoiceBot.BotId, ImmutableHashSet<string>? known = null)
    : IBotPlaying
{
    public const string BotId = "first";

    public BotProfile Profile { get; } = new(id, "First choice", "Always the first legal action.", UsesAi: false);

    public bool Knows([Required] IRulesApplying rules) => known?.Contains(rules.Id) ?? true;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);
}
