using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// A ruleset - one variant of the game - as a pure reducer: <c>(state, action) -> (state, events)</c>,
/// with no I/O and no clock. Board, pieces, seats, teams and every rule are the ruleset's to choose;
/// nothing outside it knows which it chose. A ruleset never rolls dice: it asks for them
/// (<see cref="GameState.AwaitedDice"/>), and whoever hosts the game rolls them and applies how they
/// came up as a <see cref="DiceAction"/>, so that a replay or a test supplies the rolls that happened.
/// </summary>
public interface IRulesApplying
{
    string Id { get; }

    string Name { get; }

    ICellNavigating Topology { get; }

    /// <summary>Each seat's throne, if the ruleset has thrones; empty if it has none.</summary>
    ImmutableDictionary<SeatId, CellId> Thrones { get; }

    GameState CreateInitialState();

    /// <summary>What the deciding seat may do; empty once the game is over, and while it waits on dice.</summary>
    ImmutableList<GameAction> LegalActions([Required] GameState state);

    /// <exception cref="IllegalActionException">The action is not among the legal actions.</exception>
    Transition Apply([Required] GameState state, [Required] GameAction action);

    /// <summary>
    /// Whether the deciding seat may offer a ransom now. Offers are not among the legal actions:
    /// there are too many; <see cref="Apply"/> refuses one that may not be made.
    /// </summary>
    bool MayOffer([Required] GameState state);

    /// <summary>
    /// Whether <paramref name="seat"/> may propose ending the game now. A proposal may come from any seat,
    /// whoever decides: <see cref="Apply"/> takes one from a seat this allows.
    /// </summary>
    bool MayProposeEnd([Required] GameState state, SeatId seat);

    /// <summary>
    /// Whether <paramref name="seat"/>, having given up its turns, may take them back now. Like a proposal,
    /// it may, whoever decides: <see cref="Apply"/> takes a rejoining from a seat this allows.
    /// </summary>
    bool MayRejoin([Required] GameState state, SeatId seat);

    /// <summary>
    /// Whether <paramref name="voter"/> may vote whether to stop waiting for <paramref name="absent"/>, both
    /// still playing. Like a proposal, a vote is cast whoever decides; <see cref="Apply"/> takes one this
    /// allows. When to ask for votes, and whose, is the host's to say: it knows who is a person.
    /// </summary>
    bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent);

    /// <summary>How the deciding seat's decision is timed; null if it may take as long as it likes.</summary>
    DecisionTimer? TimerOf([Required] GameState state);

    /// <exception cref="MalformedActionException">The descriptor describes no action of this ruleset.</exception>
    GameAction Parse([Required] ActionDescriptor descriptor);

    /// <summary>What a piece of <paramref name="kind"/> is worth, to one weighing a trade of prisoners.</summary>
    int WorthOf(PieceKindId kind);

    /// <summary>
    /// The cells the pieces of <paramref name="seat"/>'s own army could capture on, whatever stands on
    /// them, the dice and whose turn it is aside: for one weighing what a move leaves open.
    /// </summary>
    ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat);

    /// <summary>Whether a piece of <paramref name="kind"/> leads its army, as a Chaturaji 2027 Raja does.</summary>
    bool IsLeader(PieceKindId kind);

    string NameOf(PieceKindId kind);

    string NameOf(SeatId seat);

    /// <summary>What a die showing <paramref name="value"/> lets its roller do, for people: "Horse".</summary>
    string NameOfDie(int value);

    /// <summary>
    /// Whether the deciding seat could use a die rolled showing <paramref name="value"/> now: false if
    /// nothing it may move, promote or bring back is let by it.
    /// </summary>
    bool MayUseDie([Required] GameState state, int value);
}
