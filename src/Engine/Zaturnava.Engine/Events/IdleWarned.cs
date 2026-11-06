using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// With <see cref="Seat"/> passing, no one has moved for <see cref="Rounds"/> rounds: should no one move in the
/// round to come either, the game ends on points.
/// </summary>
public sealed record IdleWarned(SeatId Seat, int Rounds) : GameEvent;
