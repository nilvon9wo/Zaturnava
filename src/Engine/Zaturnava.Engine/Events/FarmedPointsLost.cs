using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Farmer"/>'s leader was taken while it farmed a throne: <see cref="Farmer"/> lost the
/// <see cref="Lost"/> points it farmed, and <see cref="Captor"/> scored <see cref="Gained"/>.
/// </summary>
public sealed record FarmedPointsLost(SeatId Farmer, SeatId Captor, int Lost, int Gained) : GameEvent;
