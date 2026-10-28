using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Let the ally <see cref="Regent"/> take the rest of the turn, with the actor's own army only.</summary>
public sealed record HandOverAction(SeatId Actor, SeatId Regent) : GameAction(Actor)
{
    public const string Kind = "hand-over";
    public const string RegentArgument = "regent";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(RegentArgument, Text(this.Regent.Value)));

    public static HandOverAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), new SeatId(descriptor.IntegerArgument(RegentArgument)));
}
