using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Seats;

/// <summary>A seat and the direction its army calls forward.</summary>
public sealed record Seat(SeatId Id, Direction Forward);
