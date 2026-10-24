using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// A ruleset - one variant of the game - as a pure reducer: <c>(state, action) -> (state, events)</c>,
/// with no I/O and no clock. Board, pieces, seats, teams and every rule are the ruleset's to choose;
/// nothing outside it knows which it chose. Chance (dice) will be handed in as a port of its own, so
/// that a replay or a test can supply the rolls that actually happened.
/// </summary>
public interface IRulesApplying
{
    string Id { get; }

    string Name { get; }

    ICellNavigating Topology { get; }

    GameState CreateInitialState();

    /// <summary>What the deciding seat may do; empty once the game is over.</summary>
    ImmutableList<GameAction> LegalActions([Required] GameState state);

    /// <exception cref="IllegalActionException">The action is not among the legal actions.</exception>
    Transition Apply([Required] GameState state, [Required] GameAction action);

    /// <exception cref="MalformedActionException">The descriptor describes no action of this ruleset.</exception>
    GameAction Parse([Required] ActionDescriptor descriptor);

    string NameOf(PieceKindId kind);

    string NameOf(SeatId seat);
}
