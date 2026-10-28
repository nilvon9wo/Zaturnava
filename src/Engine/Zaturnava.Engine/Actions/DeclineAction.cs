using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Decline the ransom offered: every prisoner stays where it is.</summary>
public sealed record DeclineAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "decline";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static DeclineAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
