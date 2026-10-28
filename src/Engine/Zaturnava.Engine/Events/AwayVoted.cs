using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Voter"/> would stop waiting for <see cref="Absent"/> (if <see cref="Stops"/>), or wait.
/// </summary>
public sealed record AwayVoted(SeatId Voter, SeatId Absent, bool Stops) : GameEvent;
