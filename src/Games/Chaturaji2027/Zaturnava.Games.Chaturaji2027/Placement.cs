using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>A piece where it starts: whose, of what kind, and on which column and row, row 0 at the bottom.</summary>
public sealed record Placement(SeatId Owner, PieceKindId Kind, int Column, int Row)
{
    public Piece PieceOf(int id) => new(new PieceId(id), this.Kind, this.Owner);
}
