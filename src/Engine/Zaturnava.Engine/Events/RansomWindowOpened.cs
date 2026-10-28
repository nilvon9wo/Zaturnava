using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Seat"/>'s turn is over, but it holds prisoners: it may offer ransoms before the turn
/// passes.
/// </summary>
public sealed record RansomWindowOpened(SeatId Seat) : GameEvent;
