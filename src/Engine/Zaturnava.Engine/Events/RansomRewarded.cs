using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Seat"/> accepted a ransom, sending <see cref="Prisoners"/> of its own prisoners home, and scored
/// <see cref="Points"/>.
/// </summary>
public sealed record RansomRewarded(SeatId Seat, int Prisoners, int Points) : GameEvent;
