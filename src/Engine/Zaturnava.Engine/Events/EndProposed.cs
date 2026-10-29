using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Proposer"/> proposed ending the game, giving up its turns meanwhile if <see cref="Resigns"/>;
/// <see cref="IsAhead"/> says that its team led on points then.
/// </summary>
public sealed record EndProposed(SeatId Proposer, bool Resigns, bool IsAhead = false) : GameEvent;
