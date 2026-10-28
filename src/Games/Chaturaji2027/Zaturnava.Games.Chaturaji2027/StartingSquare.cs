using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

internal sealed record StartingSquare(SeatId Owner, PieceKindId Kind, CellId Cell)
{
    public (CellId Cell, Piece Piece) Placed(PieceId id) => (this.Cell, new Piece(id, this.Kind, this.Owner));
}
