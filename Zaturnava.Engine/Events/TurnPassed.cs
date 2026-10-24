using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

public sealed record TurnPassed(SeatId From, SeatId To) : GameEvent;
