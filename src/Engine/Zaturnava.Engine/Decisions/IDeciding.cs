using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>
/// Whatever plays a seat: a person behind some client, a scripted bot, a trained model, or an AI
/// connected over MCP. The engine never knows which.
/// </summary>
public interface IDeciding
{
    /// <summary>One of <see cref="DecisionRequest.LegalActions"/>.</summary>
    Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// One of the chances the seat has now, whoever decides (<see cref="IRulesApplying.ChancesOf"/>), given as
    /// <see cref="DecisionRequest.LegalActions"/>; or none, letting them go by for now: none, unless it says otherwise.
    /// </summary>
    Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction?>(cancellationToken)
            : Task.FromResult<GameAction?>(null);
}
