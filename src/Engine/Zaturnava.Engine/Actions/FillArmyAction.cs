using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Have the rest of one's army drafted at random, as the ruleset drafts one (its throne too, if not yet set), and be
/// done drafting.
/// </summary>
public sealed record FillArmyAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "fill-army";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static FillArmyAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
