using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Piece"/> fled the board from <see cref="Cell"/>, as <see cref="FledBy"/> chose, beyond anyone's reach
/// (<see cref="Rules.GameState.Fled"/>).
/// </summary>
public sealed record PieceFled(Piece Piece, CellId Cell, SeatId FledBy) : GameEvent;
