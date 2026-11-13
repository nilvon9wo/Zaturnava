using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Take back the piece one drafted onto <see cref="Cell"/>, its price refunded, while drafting all at once
/// (<see cref="Rules.ArmyDraft.IsSimultaneous"/>).
/// </summary>
public sealed record UndraftAction(SeatId Actor, CellId Cell) : GameAction(Actor)
{
    public const string Kind = "undraft";
    /// <summary>"home", as an escape's: a cell chosen once the button is.</summary>
    public const string CellArgument = "home";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(CellArgument, Text(this.Cell.Value)));

    public static UndraftAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.CellArgument(CellArgument));
}
