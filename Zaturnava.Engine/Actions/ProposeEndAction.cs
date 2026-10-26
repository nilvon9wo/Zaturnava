using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Propose ending the game, for the others to agree to, at any time, whoever's turn it is; if
/// <see cref="Resigns"/>, give up all the actor's turns, whatever the others answer.
/// </summary>
public sealed record ProposeEndAction(SeatId Actor, bool Resigns) : GameAction(Actor)
{
    public const string Kind = "propose-end";
    public const string ResignsArgument = "resigns";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(ResignsArgument, ActionDescriptor.Flag(this.Resigns)));

    public static ProposeEndAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.BooleanArgument(ResignsArgument));
}
