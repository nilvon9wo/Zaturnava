using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Engine.Decisions;

/// <summary>
/// Whatever plays a seat: a person behind some client, a scripted bot, a trained model, or an AI
/// connected over MCP. The engine never knows which.
/// </summary>
public interface IDeciding
{
    /// <summary>One of <see cref="DecisionRequest.LegalActions"/>.</summary>
    Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken);
}
