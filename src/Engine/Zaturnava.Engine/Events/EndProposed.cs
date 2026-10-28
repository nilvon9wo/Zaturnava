using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Proposer"/> proposed ending the game, giving up its turns meanwhile if <see cref="Resigns"/>.
/// </summary>
public sealed record EndProposed(SeatId Proposer, bool Resigns) : GameEvent;
