using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>A predictable bot that takes every chance: always the first legal action, and the first chance.</summary>
internal sealed class SeizingBot : IDeciding
{
    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.LegalActions[0]);

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<GameAction?>(request.LegalActions[0]);
}
