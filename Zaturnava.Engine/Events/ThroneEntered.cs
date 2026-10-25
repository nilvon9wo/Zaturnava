using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Raja"/> entered <see cref="Throne"/>'s throne, scoring its owner <see cref="Points"/>.</summary>
public sealed record ThroneEntered(Piece Raja, SeatId Throne, int Points) : GameEvent;
