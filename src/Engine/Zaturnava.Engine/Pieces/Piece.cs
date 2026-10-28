using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

public sealed record Piece(PieceId Id, PieceKindId Kind, SeatId Owner);
