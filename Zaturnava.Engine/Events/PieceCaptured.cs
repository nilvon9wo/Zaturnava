using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

public sealed record PieceCaptured(Piece Piece, SeatId CapturedBy, int Points) : GameEvent;
