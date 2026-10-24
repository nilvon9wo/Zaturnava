using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// Chance, as a decider: when the game waits on dice, it rolls them with <paramref name="dice"/> for
/// the seat that asked, whoever plays that seat.
/// </summary>
internal sealed class DiceRoller(IDiceRolling dice) : IDeciding
{
    private readonly IDiceRolling _dice = dice;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        DiceRequest awaited = request.State.AwaitedDice!; // the loop only asks this decider while dice are awaited
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<GameAction>(cancellationToken)
            : Task.FromResult<GameAction>(new DiceAction(request.Seat, this._dice.Roll(awaited)));
    }
}
