using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Commander"/>, on its own turn, commanded every piece on the board, scoring <see cref="Points"/>.
/// </summary>
public sealed record EveryPieceCommanded(SeatId Commander, int Points) : GameEvent;
