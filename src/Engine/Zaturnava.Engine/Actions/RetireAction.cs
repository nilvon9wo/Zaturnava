using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>End one's own part in the game, as a ruleset may let a seat left with only its leader.</summary>
public sealed record RetireAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "retire";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static RetireAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
