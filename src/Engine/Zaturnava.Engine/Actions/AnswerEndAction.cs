using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Agree to end the game now, as proposed, or refuse, and play on.</summary>
public sealed record AnswerEndAction(SeatId Actor, bool Agrees) : GameAction(Actor)
{
    public const string Kind = "answer-end";
    public const string AgreesArgument = "agrees";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(AgreesArgument, ActionDescriptor.Flag(this.Agrees)));

    public static AnswerEndAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.BooleanArgument(AgreesArgument));
}
