using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// A bot that, while the armies are drafted, has its own filled at random (Proposed: until each bot drafts its own
/// way, as the roadmap's Midas by value, Solomon evenly), and plays as itself once play begins.
/// </summary>
public sealed class FillingArmies([Required] IDeciding bot) : IDeciding
{
    private readonly IDeciding _bot = bot;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        request.LegalActions.OfType<FillArmyAction>().FirstOrDefault() is { } fill
            ? Task.FromResult<GameAction>(fill)
            : this._bot.Decide(request, cancellationToken);

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        this._bot.Seize(request, cancellationToken);
}
