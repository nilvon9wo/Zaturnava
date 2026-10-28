using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Promote the Pawn on <see cref="Cell"/> into the captured piece <see cref="Into"/>.</summary>
public sealed record PromoteAction(SeatId Actor, CellId Cell, PieceId Into) : GameAction(Actor)
{
    public const string Kind = "promote";
    public const string CellArgument = "cell";
    public const string IntoArgument = "into";

    public override ActionDescriptor Describe()
    {
        string cell = Text(this.Cell.Value);
        string into = Text(this.Into.Value);
        return new ActionDescriptor(Kind, this.ActorArguments().Add(CellArgument, cell).Add(IntoArgument, into));
    }

    public static PromoteAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId cell = descriptor.CellArgument(CellArgument);
        PieceId into = new(descriptor.IntegerArgument(IntoArgument));
        return new PromoteAction(actor, cell, into);
    }
}
