using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// The piece on <see cref="Cell"/>, which may flee from there, leaves the board, where none may take it, as the King of
/// Tafl escapes at a corner (<see cref="Rules.GameState.Fled"/>).
/// </summary>
public sealed record FleeAction(SeatId Actor, CellId Cell) : GameAction(Actor)
{
    public const string Kind = "flee";
    public const string CellArgument = "cell";

    public override ActionDescriptor Describe() =>
        new(Kind, this.ActorArguments().Add(CellArgument, Text(this.Cell.Value)));

    public static FleeAction Parse([Required] ActionDescriptor descriptor) =>
        new(descriptor.Actor(), descriptor.CellArgument(CellArgument));
}
