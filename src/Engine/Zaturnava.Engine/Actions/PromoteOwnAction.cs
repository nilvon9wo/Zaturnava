using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Promote the piece on <see cref="PromotionAction.Cell"/> by its own promotion: it becomes a piece of kind
/// <see cref="Becomes"/>, still its army's.
/// </summary>
public sealed record PromoteOwnAction(SeatId Actor, CellId Cell, PieceKindId Becomes) : PromotionAction(Actor, Cell)
{
    public const string Kind = "promote-own";
    public const string CellArgument = "cell";
    public const string BecomesArgument = "becomes";

    public override ActionDescriptor Describe()
    {
        string cell = Text(this.Cell.Value);
        return new ActionDescriptor(
            Kind,
            this.ActorArguments().Add(CellArgument, cell).Add(BecomesArgument, this.Becomes.Value)
        );
    }

    /// <exception cref="MalformedActionException">The argument naming what it becomes is missing.</exception>
    public static PromoteOwnAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId cell = descriptor.CellArgument(CellArgument);
        string becomes = descriptor.Arguments.GetValueOrDefault(BecomesArgument, string.Empty);
        return becomes.Length > 0
            ? new PromoteOwnAction(actor, cell, new PieceKindId(becomes))
            : throw new MalformedActionException($"The '{Kind}' action needs a kind for '{BecomesArgument}'.");
    }
}
