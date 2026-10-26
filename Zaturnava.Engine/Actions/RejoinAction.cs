using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Take back the turns given up in proposing to end the game, whoever's turn it is.</summary>
public sealed record RejoinAction(SeatId Actor) : GameAction(Actor)
{
    public const string Kind = "rejoin";

    public override ActionDescriptor Describe() => new(Kind, this.ActorArguments());

    public static RejoinAction Parse([Required] ActionDescriptor descriptor) => new(descriptor.Actor());
}
