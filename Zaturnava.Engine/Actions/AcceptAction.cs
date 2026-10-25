using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Accept the ransom offered: the prisoners on both sides go home.</summary>
public sealed record AcceptAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "accept";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static AcceptAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
