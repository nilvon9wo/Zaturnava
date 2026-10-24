using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="CapturedBy"/> took <see cref="Raja"/> on its own throne, scoring <see cref="Points"/>
/// on top of the capture itself.
/// </summary>
public sealed record RajaCapturedOnThrone(Piece Raja, SeatId CapturedBy, int Points) : GameEvent;
