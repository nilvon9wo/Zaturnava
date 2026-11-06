using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Promote the piece on <see cref="Cell"/>: into a captured piece (<see cref="PromoteAction"/>), or as its own
/// promotion has it (<see cref="PromoteOwnAction"/>).
/// </summary>
public abstract record PromotionAction(SeatId Actor, CellId Cell) : GameAction(Actor);
