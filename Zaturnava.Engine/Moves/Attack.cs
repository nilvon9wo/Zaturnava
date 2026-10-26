using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Moves;

/// <summary>A cell a piece could capture on, whatever stands there: an enemy to take, or a friend to guard.</summary>
public sealed record Attack(Piece Attacker, CellId From, CellId Target);
