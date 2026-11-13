using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> is done drafting its army, keeping <see cref="Kept"/> points to start with.</summary>
public sealed record DraftFinished(SeatId Seat, int Kept) : GameEvent;
