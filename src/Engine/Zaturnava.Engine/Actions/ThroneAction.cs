using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>Set one's throne on <see cref="Cell"/>, while drafting one's army: the first thing a seat drafts.</summary>
public sealed record ThroneAction(SeatId Actor, CellId Cell) : GameAction(Actor)
{
    public const string Kind = "throne";
    /// <summary>"home", as an escape's: a cell chosen once the button is.</summary>
    public const string CellArgument = "home";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(CellArgument, Text(this.Cell.Value)));

    public static ThroneAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.CellArgument(CellArgument));
}
