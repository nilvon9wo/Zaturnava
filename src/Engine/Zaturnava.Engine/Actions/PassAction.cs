using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Decline to move: moving is never mandatory.</summary>
public sealed record PassAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "pass";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static PassAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
