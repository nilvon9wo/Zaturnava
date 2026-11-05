using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Call a leader face-off: the leader on <see cref="Caller"/> and the enemy leader on <see cref="Faced"/>, facing
/// each other, each taken by the other's side at once. A chance, not a turn: it may be taken whoever decides.
/// </summary>
public sealed record FaceOffAction(SeatId Actor, CellId Caller, CellId Faced) : GameAction(Actor)
{
    public const string Kind = "face-off";
    public const string CallerArgument = "caller";
    public const string FacedArgument = "faced";

    public override ActionDescriptor Describe()
    {
        string caller = Text(this.Caller.Value);
        string faced = Text(this.Faced.Value);
        return new ActionDescriptor(
            Kind,
            this.ActorArguments().Add(CallerArgument, caller).Add(FacedArgument, faced)
        );
    }

    public static FaceOffAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        CellId caller = descriptor.CellArgument(CallerArgument);
        CellId faced = descriptor.CellArgument(FacedArgument);
        return new FaceOffAction(actor, caller, faced);
    }
}
