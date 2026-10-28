using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// The last enemy Raja left the board while <see cref="Creator"/> commanded an ally's army: it created the
/// empire, scoring <see cref="Points"/>.
/// </summary>
public sealed record EmpireCreated(SeatId Creator, int Points) : GameEvent;
