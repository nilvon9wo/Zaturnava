using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Say whether to stop waiting for <see cref="Absent"/>, who is taking long to decide (if
/// <see cref="Stops"/>), or to keep waiting; the vote stands until changed. Cast whoever's turn it is.
/// </summary>
public sealed record VoteAwayAction(SeatId Actor, SeatId Absent, bool Stops) : GameAction(Actor)
{
    public const string Kind = "vote-away";
    public const string AbsentArgument = "absent";
    public const string StopsArgument = "stops";

    public override ActionDescriptor Describe() =>
        new(
            Kind,
            this.ActorArguments()
                .Add(AbsentArgument, this.Absent.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Add(StopsArgument, ActionDescriptor.Flag(this.Stops))
        );

    public static VoteAwayAction Parse([Required] ActionDescriptor descriptor) =>
        new(
            descriptor.Actor(),
            new SeatId(descriptor.IntegerArgument(AbsentArgument)),
            descriptor.BooleanArgument(StopsArgument)
        );
}
