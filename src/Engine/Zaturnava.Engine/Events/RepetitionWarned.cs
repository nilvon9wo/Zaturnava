using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// For <see cref="Rounds"/> rounds, every turn has ended where the game has been before: should the round to come
/// do so too, the game ends on points. <see cref="Seat"/> is the seat to play.
/// </summary>
public sealed record RepetitionWarned(SeatId Seat, int Rounds) : GameEvent;
