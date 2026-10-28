using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="MovedBy"/> moved <see cref="Pawn"/> from <see cref="From"/> to <see cref="To"/>, swapping places with
/// <see cref="Other"/>, an allied Pawn that stood there back to back with it.
/// </summary>
public sealed record PawnsSwapped(Piece Pawn, Piece Other, CellId From, CellId To, SeatId MovedBy) : GameEvent;
