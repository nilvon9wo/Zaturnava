using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="MovedBy"/> moved <see cref="Piece"/>, which need not be their own.</summary>
public sealed record PieceMoved(Piece Piece, CellId From, CellId To, SeatId MovedBy) : GameEvent;
