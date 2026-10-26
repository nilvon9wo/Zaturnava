using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Put the piece waiting to come onto the board (one going home from a ransom) on <see cref="To"/>.</summary>
public sealed record PlaceAction(SeatId Actor, CellId To) : GameAction(Actor)
{
    public const string Kind = "place";
    public const string ToArgument = "to";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(ToArgument, Text(this.To.Value)));

    public static PlaceAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.CellArgument(ToArgument));
}
