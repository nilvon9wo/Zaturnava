using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="HiredBy"/> hired the mercenary <see cref="Piece"/>, as it was while a prisoner, for <see cref="Cost"/>
/// points (<see cref="Rules.GameState.Hired"/>).
/// </summary>
public sealed record PieceHired(Piece Piece, SeatId HiredBy, int Cost) : GameEvent;
