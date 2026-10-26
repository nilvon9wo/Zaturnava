using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// The deciding seat, away, passes, as the others voted: what the host decides for it, never a player.
/// </summary>
public sealed record PassAwayAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "pass-away";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static PassAwayAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
