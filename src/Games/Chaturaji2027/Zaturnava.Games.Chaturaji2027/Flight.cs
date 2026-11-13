using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The Tafl King's escape (Stated, 2026-11-13: "If I move him to z26 (the far corner square, and only the far corner
/// square), then I can optionally remove him from the board so nobody can ever capture him. He can come back into the
/// game only if I roll doubles allowing me to return a leader to the board"): a piece of <paramref name="fleers"/>
/// standing on its own army's far corner, the board's corner across from its kingdom's, may flee the board, as a move
/// of it, using a die that moves it, or none at the start of a turn, as any move (Proposed). It is then no one's
/// prisoner, so none may ransom it, nor take it, nor promote into it, and it scores nothing; it comes back as a
/// prisoner escapes, on doubles that free its kind.
/// </summary>
internal sealed class Flight(SquareGridTopology grid, int width, int height, ImmutableHashSet<PieceKindId> fleers)
{
    private static readonly PieceKindId Corner = new("far-corner");

    private readonly ImmutableHashSet<PieceKindId> _fleers = fleers;

    /// <summary>Each seat's far corner, by seat: seat 0's the top right, turned for each next seat.</summary>
    private readonly ImmutableList<CellId> _corners =
    [
        .. Enumerable.Range(0, Pinwheel.SeatCount)
            .Select(seat => Pinwheel.At(width, height, seat, Far(Pinwheel.FrameOf(width, height, seat))))
            .Select(corner => grid.CellAt(corner.Column, corner.Row)),
    ];

    /// <summary>
    /// The flights the seat may choose: each piece that may flee, of an army it commands, but a retired seat's, on its
    /// own army's far corner, of a kind <paramref name="isMovable"/>.
    /// </summary>
    public IEnumerable<FleeAction> FleesOf(
        GameState state,
        SeatId seat,
        ImmutableHashSet<SeatId> armies,
        Func<PieceKindId, bool> isMovable
    ) =>
        state.Position.Occupants
            .Where(occupant => this._fleers.Contains(occupant.Value.Kind) && isMovable(occupant.Value.Kind))
            .Where(occupant => armies.Contains(occupant.Value.Owner) && !state.Retired.Contains(occupant.Value.Owner))
            .Where(occupant => occupant.Key == this._corners[occupant.Value.Owner.Value])
            .Select(occupant => new FleeAction(seat, occupant.Key));

    /// <summary>The piece leaves the board, for no one to take.</summary>
    public static Transition Fled(GameState state, FleeAction flee)
    {
        Piece piece = state.Position.PieceAt(flee.Cell)!; // a flight is legal only from where a piece stands
        GameState fled = state with
        {
            Position = new Position(state.Position.Occupants.Remove(flee.Cell)),
            Fled = state.Fled.Add(piece),
        };
        return new Transition(fled, [new PieceFled(piece, flee.Cell, flee.Actor)]);
    }

    private static ArmyPiece Far((int Width, int Height) frame) => new(Corner, frame.Width - 1, frame.Height - 1);
}
