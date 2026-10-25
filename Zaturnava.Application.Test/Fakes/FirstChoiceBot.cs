using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>A predictable bot: always the first legal action.</summary>
internal sealed class FirstChoiceBot : IBotPlaying
{
    public const string BotId = "first";

    public BotProfile Profile { get; } = new(BotId, "First choice", "Always the first legal action.", UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);
}
