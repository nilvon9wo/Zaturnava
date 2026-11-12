using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

/// <summary>
/// Rules of nothing but a board and its pieces: a <paramref name="width"/> by <paramref name="height"/> grid, or
/// <paramref name="topology"/>; the kinds <paramref name="kinds"/> name, a <see cref="King"/> leading; two seats a
/// side, 0 and 2 facing north and 1 and 3 south, 0 and 2 against 1 and 3.
/// </summary>
internal sealed class GridRules(
    ImmutableList<PieceKind> kinds,
    int width = 8,
    int height = 8,
    ICellNavigating? topology = null
) : IRulesApplying
{
    public static readonly PieceKindId King = new("king");

    private readonly ImmutableDictionary<PieceKindId, PieceKind> _kinds = kinds.ToImmutableDictionary(kind => kind.Id);

    public string Id => "grid";

    public string Name => "Grid";

    public ICellNavigating Topology { get; } = topology ?? new SquareGridTopology(width, height);

    public ImmutableDictionary<SeatId, CellId> Thrones => throw new NotSupportedException();

    /// <summary>The game with <paramref name="pieces"/> where they stand, by the cell's column and row.</summary>
    public static GameState State(params (int Column, int Row, string Kind, int Seat)[] pieces) =>
        new(
            new Position(pieces
                .Select((piece, at) => (piece, at))
                .ToImmutableDictionary(
                    entry => new CellId((entry.piece.Row * 8) + entry.piece.Column),
                    entry => new Piece(
                        new PieceId(entry.at),
                        new PieceKindId(entry.piece.Kind),
                        new SeatId(entry.piece.Seat)
                    )
                )
            ),
            [
                new Seat(new SeatId(0), new Direction(0, 1)),
                new Seat(new SeatId(1), new Direction(0, -1)),
                new Seat(new SeatId(2), new Direction(0, 1)),
                new Seat(new SeatId(3), new Direction(0, -1)),
            ],
            TeamGraph.FromTeams([[new SeatId(0), new SeatId(2)], [new SeatId(1), new SeatId(3)]]),
            new SeatId(0),
            1,
            [],
            []
        );

    public PieceGuide GuideOf(PieceKindId kind) => new(this._kinds[kind]);

    public bool IsLeader(PieceKindId kind) => kind == King;

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => throw new NotSupportedException();

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        throw new NotSupportedException();

    public bool MayOffer([Required] GameState state) => throw new NotSupportedException();

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        throw new NotSupportedException();

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayRejoin([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        throw new NotSupportedException();

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => throw new NotSupportedException();

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        throw new NotSupportedException();

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
        throw new NotSupportedException();

    public bool IsHeavy(PieceKindId kind) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => throw new NotSupportedException();
}
