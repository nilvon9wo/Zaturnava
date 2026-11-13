using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// While the armies are drafted, <see cref="Seat"/> had the rest of its army filled at random, gaining
/// <see cref="Added"/> pieces.
/// </summary>
public sealed record ArmyFilledAtRandom(SeatId Seat, int Added) : GameEvent;
