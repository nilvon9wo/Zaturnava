using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Choose to roll the dice rather than move deliberately; how they come up follows as a
/// <see cref="DiceAction"/>.
/// </summary>
public sealed record RollAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "roll";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static RollAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
