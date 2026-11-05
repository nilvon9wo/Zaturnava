using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="CalledBy"/> called a face-off: <see cref="Caller"/> and <see cref="Faced"/> taken.</summary>
public sealed record LeadersFacedOff(Piece Caller, Piece Faced, SeatId CalledBy) : GameEvent;
