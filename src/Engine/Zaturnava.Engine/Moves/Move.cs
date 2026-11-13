using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Moves;

public sealed record Move(Piece Mover, CellId From, CellId To, Piece? Captured)
{
    /// <summary>
    /// The cells of the pieces it takes by jumping over them (<see cref="MovementRule.IsJumping"/>), in the order
    /// it jumps them; <see cref="Captured"/> is the first. None, for any other move.
    /// </summary>
    public ImmutableList<CellId> Jumped { get; init; } = [];
}
