using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>A predictable bot: always the first legal action; its id, <paramref name="id"/>.</summary>
internal sealed class FirstChoiceBot(string id = FirstChoiceBot.BotId) : IBotPlaying
{
    public const string BotId = "first";

    public BotProfile Profile { get; } = new(id, "First choice", "Always the first legal action.", UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);
}
