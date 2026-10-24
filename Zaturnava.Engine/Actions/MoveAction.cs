using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Move the piece on <see cref="From"/> to <see cref="To"/>, capturing whatever is there.</summary>
public sealed record MoveAction(SeatId Actor, CellId From, CellId To) : GameAction(Actor)
{
    public const string Kind = "move";
    public const string FromArgument = "from";
    public const string ToArgument = "to";

    public override ActionDescriptor Describe()
    {
        string from = Text(this.From.Value);
        string to = Text(this.To.Value);
        return new ActionDescriptor(Kind, this.ActorArguments().Add(FromArgument, from).Add(ToArgument, to));
    }

    public static MoveAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId from = descriptor.CellArgument(FromArgument);
        CellId to = descriptor.CellArgument(ToArgument);
        return new MoveAction(actor, from, to);
    }
}
