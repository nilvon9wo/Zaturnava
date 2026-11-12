using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Place one's palace about <see cref="Cell"/>, its centre, while drafting one's army: in a game with palaces, the
/// first thing a seat drafts, before its throne.
/// </summary>
public sealed record PalaceAction(SeatId Actor, CellId Cell) : GameAction(Actor)
{
    public const string Kind = "palace";

    /// <summary>"home", as a throne's: a cell chosen once the button is.</summary>
    public const string CellArgument = "home";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(CellArgument, Text(this.Cell.Value)));

    public static PalaceAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.CellArgument(CellArgument));
}
