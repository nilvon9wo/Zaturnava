using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// No piece has been taken for <see cref="Rounds"/> rounds: should none be taken in the <see cref="RoundsLeft"/>
/// to come, the game ends on points. <see cref="Seat"/> is the seat to play.
/// </summary>
public sealed record QuietWarned(SeatId Seat, int Rounds, int RoundsLeft) : GameEvent;
