using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Positions;

/// <summary>Which piece stands on which cell. Immutable: every change derives a new position.</summary>
public sealed record Position(ImmutableDictionary<CellId, Piece> Occupants)
{
    public static readonly Position Empty = new([]);

    public Piece? PieceAt(CellId cell) => this.Occupants.GetValueOrDefault(cell);

    public Position Place(CellId cell, [Required] Piece piece) =>
        new(this.Occupants.SetItem(cell, piece));

    /// <summary>Moves the piece on <paramref name="from"/> to <paramref name="to"/>, replacing any occupant.</summary>
    public Position Relocate(CellId from, CellId to)
    {
        Piece mover = this.Occupants[from];
        ImmutableDictionary<CellId, Piece> vacated = this.Occupants.Remove(from);
        return new Position(vacated.SetItem(to, mover));
    }
}
