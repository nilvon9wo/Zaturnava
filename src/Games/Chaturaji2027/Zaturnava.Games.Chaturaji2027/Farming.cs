using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Farming a throne (rules.md §5): a Raja stepping off an enemy throne idly - taking nothing, and attacking or
/// guarding no piece from where it lands - and back on again, its player moving nothing else meanwhile. The
/// points such entries score are farmed, at risk until its player does anything else; were the Raja taken
/// meanwhile, its player loses them, and its captor scores twice as much.
/// </summary>
internal sealed class Farming(ThroneControl control, MoveGenerator moves)
{
    private readonly ThroneControl _control = control;
    private readonly MoveGenerator _moves = moves;

    /// <summary>What a move does to its actor's farming: farms more, steps off idly, or stops farming.</summary>
    public Transition Tallied(GameState before, MoveAction move, Transition moved)
    {
        GameState state = moved.State;
        SeatId farmer = move.Actor;
        Piece mover = before.Position.PieceAt(move.From)!; // the move is legal, so a piece stands there
        bool isOwnRaja = mover.Owner == farmer
            && this._control.IsLeader(mover.Kind)
            && before.Position.PieceAt(move.To) is null;
        bool isEntry = moved.Events.OfType<ThroneEntered>().Any();
        bool isIdlyOff = this.IsEnemyThrone(state, move.From, farmer) && this.IsIdle(state, move.To, farmer);
        GameState tallied = (isOwnRaja, isEntry, isIdlyOff, state.SteppedOff.Contains(farmer)) switch
        {
            (true, true, _, true) => state with
            {
                Farmed = state.Farmed.SetItem(
                    farmer,
                    state.Farmed.GetValueOrDefault(farmer) + Chaturaji2027Rules.ThroneEntryBonus
                ),
                SteppedOff = state.SteppedOff.Remove(farmer),
            },
            (true, false, true, _) => state with { SteppedOff = state.SteppedOff.Add(farmer) },
            _ => Stopped(state, farmer),
        };
        return moved with { State = tallied };
    }

    /// <summary>The seat stops farming: what it farmed is safe.</summary>
    public static GameState Stopped(GameState state, SeatId seat) =>
        state with { Farmed = state.Farmed.Remove(seat), SteppedOff = state.SteppedOff.Remove(seat) };

    /// <summary>
    /// A Raja taken while its player farmed: its player loses the farmed points, and its captor, if an enemy,
    /// scores twice as much.
    /// </summary>
    public Transition Forfeited(Transition taken, Piece captured, SeatId captor, bool isEnemy)
    {
        GameState state = taken.State;
        SeatId farmer = captured.Owner;
        int farmed = this._control.IsLeader(captured.Kind) ? state.Farmed.GetValueOrDefault(farmer) : 0;
        int gained = isEnemy ? 2 * farmed : 0;
        GameState settled = Stopped(state, farmer) with
        {
            Scores = state.Scores
                .SetItem(farmer, state.Scores[farmer] - farmed)
                .SetItem(captor, state.Scores[captor] + gained),
        };
        return farmed > 0
            ? new Transition(settled, taken.Events.Add(new FarmedPointsLost(farmer, captor, farmed, gained)))
            : taken;
    }

    private bool IsEnemyThrone(GameState state, CellId cell, SeatId seat) =>
        this._control.OwnerOf(cell) is { } owner && !state.Teams.AreAllied(owner, seat);

    /// <summary>Whether the Raja on <paramref name="cell"/> attacks and guards no piece from there.</summary>
    private bool IsIdle(GameState state, CellId cell, SeatId seat) =>
        this._moves.AttacksFrom(state.Position, cell, state.SeatOf(seat))
            .All(attack => state.Position.PieceAt(attack.Target) is null);
}
