using System.Collections.Concurrent;
using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Each kind's <see cref="MoveDiagram"/> (Proposed), found by the same move generator a game's moves are: the piece
/// alone on an empty board <see cref="Side"/> cells square, on its centre, facing up; where it may move, there; and
/// where it may capture, an enemy put on each cell in turn. A kind that hops is drawn with a piece of its own two
/// cells out along each line it hops on, to hop over; one a river holds back (<paramref name="riverBound"/>), with a
/// river just ahead of it, and nothing across it (the user's report of 2026-11-12: "Pictures speak 1000 words!").
/// One that steps along a palace's lines (<paramref name="palaceSteppers"/>) stands on the throne of a palace about it,
/// stepping along each line to the next cell, as <see cref="PalaceSteps"/> lets it, those of
/// <paramref name="forwardPalaceSteppers"/> only forward; either is drawn on an open board too, for the player to
/// switch to. Pieces put in its way, where they stop it most, and, for a kind of <paramref name="custodians"/>, a
/// capture by custody, are drawn for the player to ask for. What else the board or the game's rules add (the two-cell
/// first move) is not drawn: the kind's powers and restrictions tell of it.
/// </summary>
internal sealed class MoveDiagrams(
    ImmutableDictionary<PieceKindId, PieceKind> kinds,
    ImmutableHashSet<PieceKindId> riverBound,
    ImmutableHashSet<PieceKindId> palaceSteppers,
    ImmutableHashSet<PieceKindId> forwardPalaceSteppers,
    ImmutableHashSet<PieceKindId> custodians,
    ImmutableHashSet<PieceKindId> sidewaysAcrossRiver
)
{
    /// <summary>As wide as the farthest leap of the catalogue, four cells, either side of the centre.</summary>
    public const int Side = 9;

    private const int Middle = Side / 2;

    private static readonly SquareGridTopology Grid = new(Side, Side);
    private static readonly CellId Centre = Grid.CellAt(Middle, Middle);
    private static readonly Seat Facing = new(new SeatId(0), new Direction(0, 1));
    private static readonly SeatId Enemy = new(1);
    private static readonly PieceKindId Stand = new("diagram-stand");

    /// <summary>A palace's lines on the diagram's board, whoever steps along them.</summary>
    private static readonly PalaceSteps Lines = new(Grid, [], []);

    /// <summary>The 3 × 3 palace about the centre, its throne, row by row from the bottom.</summary>
    private static readonly ImmutableList<CellId> Palace =
    [
        .. Enumerable.Range(Middle - 1, 3)
            .SelectMany(row => Enumerable.Range(Middle - 1, 3).Select(column => Grid.CellAt(column, row))),
    ];

    private readonly ImmutableDictionary<PieceKindId, PieceKind> _kinds = kinds;
    private readonly MoveGenerator _moves = new(Grid, kinds);
    private readonly ConcurrentDictionary<PieceKindId, MoveDiagram> _drawn = new();
    private readonly ConcurrentDictionary<PieceKindId, MoveDiagram> _open = new();

    public MoveDiagram Of(PieceKindId kind) => this._drawn.GetOrAdd(kind, this.DrawWithTerrain);

    /// <summary>
    /// A kind that steps along a palace's lines drawn in a palace of <paramref name="palace"/>'s cells, its throne on
    /// <paramref name="throne"/>, each as far to the piece's right and ahead of it as the piece sees it; stepping along
    /// each line through its cell, as <see cref="PalaceSteps"/> lets it, and moving otherwise as on an open board,
    /// which it is drawn on too (the user's report of 2026-11-15, B6).
    /// </summary>
    public MoveDiagram InPalace(
        PieceKindId kind,
        ImmutableList<(int Right, int Ahead)> palace,
        (int Right, int Ahead) throne
    )
    {
        ImmutableList<CellId> cells = [.. palace.Select(At)];
        ImmutableList<int> steps =
        [
            .. Lines.NeighboursOn(cells, At(throne), Centre)
                .Where(cell => !forwardPalaceSteppers.Contains(kind) || Grid.RowOf(cell) > Middle)
                .Select(cell => cell.Value),
        ];
        MoveDiagram open = this.Open(kind);
        return open with
        {
            Moves = [.. open.Moves.Concat(steps).Distinct().Order()],
            Captures = [.. open.Captures.Concat(steps).Distinct().Order()],
            Palace = [.. cells.Select(cell => cell.Value)],
            Throne = At(throne).Value,
            Elsewhere = open,
        };
    }

    /// <summary>The cell as far to the right of the centre and ahead of it as <paramref name="seen"/> says.</summary>
    private static CellId At((int Right, int Ahead) seen) => Grid.CellAt(Middle + seen.Right, Middle + seen.Ahead);

    /// <summary>
    /// The kind drawn by its river or in its palace, if either holds it, with the kind drawn on an open board beside;
    /// one stepping sideways only across a river (sidewaysAcrossRiver), on its own side, without
    /// its sideways steps, with the kind across the river beside (the user's report of 2026-11-19, of a Xiangqi
    /// Soldier's card always stepping sideways); else on an open board alone.
    /// </summary>
    private MoveDiagram DrawWithTerrain(PieceKindId kind) =>
        (riverBound.Contains(kind) || palaceSteppers.Contains(kind), sidewaysAcrossRiver.Contains(kind)) switch
        {
            (true, _) => this.Draw(kind, isOnTerrain: true) with { Elsewhere = this.Open(kind) },
            (_, true) => OnOwnSide(this.Open(kind)) with { Across = this.Open(kind) },
            _ => this.Open(kind),
        };

    /// <summary><paramref name="across"/>, without the cells across the centre's row: forward and back only.</summary>
    private static MoveDiagram OnOwnSide(MoveDiagram across)
    {
        static ImmutableList<int> Kept(ImmutableList<int> cells) =>
            [.. cells.Where(cell => cell == Centre.Value || cell / Side != Middle)];
        return across with
        {
            Moves = Kept(across.Moves),
            Captures = Kept(across.Captures),
            Leaps = Kept(across.Leaps),
            Obstacles = Kept(across.Obstacles),
            Obstructed = Kept(across.Obstructed),
            CaptureObstacles = Kept(across.CaptureObstacles),
            CaptureObstructed = Kept(across.CaptureObstructed),
        };
    }

    /// <summary>The kind drawn on an open board, no river nor palace about it.</summary>
    private MoveDiagram Open(PieceKindId kind) =>
        this._open.GetOrAdd(kind, open => this.Draw(open, isOnTerrain: false));

    private MoveDiagram Draw(PieceKindId kind, bool isOnTerrain)
    {
        ImmutableList<CellId> screens = ScreensFor(this._kinds[kind]);
        Position alone = screens.Aggregate(
            Position.Empty.Place(Centre, new Piece(new PieceId(0), kind, Facing.Id)),
            (position, screen) => position.Place(screen, new Piece(new PieceId(1), Stand, Facing.Id))
        );
        int? riverRow = isOnTerrain && riverBound.Contains(kind) ? Middle + 1 : null;
        ImmutableHashSet<CellId> reached = this.ReachedOn(alone, riverRow);
        ImmutableList<Move> jumps = this.JumpsOn(alone, this._kinds[kind]);
        ImmutableList<CellId> palace = isOnTerrain && palaceSteppers.Contains(kind) ? Palace : [];
        ImmutableList<CellId> obstacles = ObstaclesOn(alone, riverRow, position => this.ReachedOn(position, riverRow));
        ImmutableHashSet<CellId> unobstructed = this.ReachedOn(WithStands(alone, obstacles), riverRow);
        ImmutableHashSet<CellId> captures =
        [
            .. Grid.Cells.Where(cell =>
                alone.PieceAt(cell) is null && IsOnNearBank(cell, riverRow) && this.MayCapture(alone, cell)
            ),
        ];
        ImmutableList<CellId> captureObstacles = ObstaclesOn(alone, riverRow, CapturesOn);
        ImmutableHashSet<CellId> captureObstructed =
            captures.Except(captureObstacles).Except(CapturesOn(WithStands(alone, captureObstacles)));
        ImmutableList<CellId> palaceSteps =
        [
            .. palace
                .Where(cell => cell != Centre)
                .Where(cell => !forwardPalaceSteppers.Contains(kind) || Grid.RowOf(cell) > Middle),
        ];
        return new MoveDiagram(
            Side,
            [.. reached.Concat(palaceSteps).Select(cell => cell.Value).Distinct().Order()],
            [
                .. Grid.Cells
                    .Where(cell => alone.PieceAt(cell) is null)
                    .Where(cell => IsOnNearBank(cell, riverRow))
                    .Where(cell => this.MayCapture(alone, cell))
                    .Concat(jumps.SelectMany(jump => jump.Jumped))
                    .Concat(palaceSteps)
                    .Select(cell => cell.Value)
                    .Distinct()
                    .Order(),
            ],
            [.. screens.Select(screen => screen.Value)]
        )
        {
            Jumps = [.. jumps.Select(jump => jump.To.Value).Distinct().Order()],
            Palace = [.. palace.Select(cell => cell.Value)],
            RiverRow = riverRow,
            Obstacles = [.. obstacles.Select(cell => cell.Value).Order()],
            Obstructed =
            [
                .. reached.Except(obstacles).Except(unobstructed).Select(cell => cell.Value).Order(),
            ],
            Custody = custodians.Contains(kind) ? CustodyOn(reached) : null,
            Berserk = custodians.Contains(kind) ? BerserkOn(reached) : [],
            Leaps =
            [
                .. reached.Where(cell => IsFar(cell) && this.IsLeapt(alone, cell)).Select(cell => cell.Value).Order(),
            ],
            CaptureObstacles = [.. captureObstacles.Select(cell => cell.Value).Order()],
            CaptureObstructed = [.. captureObstructed.Select(cell => cell.Value).Order()],
        };

        // The cells of its captures it may still capture on, with the pieces of position in its way.
        ImmutableHashSet<CellId> CapturesOn(Position position) =>
            [.. captures.Where(cell => position.PieceAt(cell) is null && this.MayCapture(position, cell))];
    }

    /// <summary>Whether <paramref name="cell"/> is farther from the centre than the cells around it.</summary>
    private static bool IsFar(CellId cell) =>
        Math.Max(Math.Abs(Grid.ColumnOf(cell) - Middle), Math.Abs(Grid.RowOf(cell) - Middle)) > 1;

    /// <summary>
    /// Whether the piece reaches <paramref name="target"/> by leaping: still, with every other cell filled, so that
    /// nothing it could slide through is left empty.
    /// </summary>
    private bool IsLeapt(Position alone, CellId target)
    {
        Position crowded = Grid.Cells
            .Where(cell => cell != target && alone.PieceAt(cell) is null)
            .Aggregate(alone, (position, cell) => position.Place(cell, new Piece(new PieceId(4), Stand, Facing.Id)));
        return this.MovesOn(crowded).Any(move => move.To == target);
    }

    /// <summary>The empty cells the piece may move to, short of the river, if any.</summary>
    private ImmutableHashSet<CellId> ReachedOn(Position position, int? riverRow) =>
    [
        .. this.MovesOn(position)
            .Where(move => move.Captured is null)
            .Select(move => move.To)
            .Where(cell => IsOnNearBank(cell, riverRow)),
    ];

    /// <summary>
    /// For a kind pieces in its way stop, as a Rook's slide or a Xiangqi Horse's leg is (the user's report of
    /// 2026-11-15, C2: pieces in the way, not red crosses): the cell a piece on which would stop it reaching the most
    /// other cells (<paramref name="reachedOn"/>: where it moves, or where it captures), the foremost of those, then
    /// the nearest the middle column, then the leftmost; and a second such cell, chosen the same way, if a piece there
    /// would stop it reaching yet more. None, for a kind nothing stops.
    /// </summary>
    private static ImmutableList<CellId> ObstaclesOn(
        Position alone,
        int? riverRow,
        Func<Position, ImmutableHashSet<CellId>> reachedOn
    ) =>
        Enumerable.Range(0, 2).Aggregate(
            ImmutableList<CellId>.Empty,
            (chosen, _) =>
            {
                Position stood = WithStands(alone, chosen);
                ImmutableHashSet<CellId> before = reachedOn(stood);
                (CellId Cell, int Stopped)? best = Grid.Cells
                    .Where(cell => stood.PieceAt(cell) is null && IsOnNearBank(cell, riverRow))
                    .Select(cell => (
                        Cell: cell,
                        Stopped: before.Remove(cell).Except(reachedOn(WithStands(stood, [cell]))).Count
                    ))
                    .Where(candidate => candidate.Stopped > 0)
                    .OrderByDescending(candidate => candidate.Stopped)
                    .ThenByDescending(candidate => Grid.RowOf(candidate.Cell))
                    .ThenBy(candidate => Math.Abs(Grid.ColumnOf(candidate.Cell) - Middle))
                    .ThenBy(candidate => Grid.ColumnOf(candidate.Cell))
                    .Select(candidate => ((CellId, int)?)candidate)
                    .FirstOrDefault();
                return best is { } found ? chosen.Add(found.Cell) : chosen;
            }
        );

    /// <summary><paramref name="position"/>, with a piece of the piece's own side on each of
    /// <paramref name="cells"/>.</summary>
    private static Position WithStands(Position position, IEnumerable<CellId> cells) =>
        cells.Aggregate(position, (stood, cell) => stood.Place(cell, new Piece(new PieceId(3), Stand, Facing.Id)));

    /// <summary>
    /// For a kind that takes by custody: it moves two cells forward, if it may, else one, next to an enemy just
    /// beyond, which a piece of its own side beyond that holds with it; none, if it may move forward neither.
    /// </summary>
    private static CustodyExample? CustodyOn(ImmutableHashSet<CellId> reached) =>
        ((int[])[2, 1])
            .Where(distance => reached.Contains(Grid.CellAt(Middle, Middle + distance)))
            .Select(distance => new CustodyExample(
                Grid.CellAt(Middle, Middle + distance).Value,
                Grid.CellAt(Middle, Middle + distance + 1).Value,
                Grid.CellAt(Middle, Middle + distance + 2).Value
            ))
            .FirstOrDefault();

    /// <summary>
    /// For a kind that takes by custody, gone berserk: landing where <see cref="CustodyOn"/> does, a piece taken just
    /// ahead and another just to its right, each held against a piece beyond, whoever's either is; none, if it may
    /// move forward nowhere.
    /// </summary>
    private static ImmutableList<CustodyExample> BerserkOn(ImmutableHashSet<CellId> reached) =>
    [
        .. new[] { CustodyOn(reached) }
            .OfType<CustodyExample>()
            .SelectMany(custody =>
                new[] { custody, new CustodyExample(custody.Landing, custody.Landing + 1, custody.Landing + 2) }
            ),
    ];

    /// <summary>Whether <paramref name="cell"/> is short of the river at <paramref name="riverRow"/>, if any.</summary>
    private static bool IsOnNearBank(CellId cell, int? riverRow) => Grid.RowOf(cell) < (riverRow ?? Side);

    /// <summary>
    /// A piece to hop over two cells out along each direction the kind hops (the user's report of 2026-11-12, of a
    /// Cannon's card showing it hop forward only); none, for a kind that never hops.
    /// </summary>
    private static ImmutableList<CellId> ScreensFor(PieceKind kind) =>
    [
        .. kind.Movement
            .Where(rule => rule.IsHopping)
            .SelectMany(rule => Grid.Resolve(rule.Directions, Facing.Forward))
            .Distinct()
            .Select(direction => Grid.CellAt(Middle + (2 * direction.First), Middle + (2 * direction.Second)))
            .OrderBy(cell => cell.Value),
    ];

    /// <summary>
    /// For a kind that jumps: its jumps with an enemy one and three cells out along each line it jumps on, so that it
    /// is drawn landing beyond the first, and then, jumping again, beyond the second; none, for any other kind.
    /// </summary>
    private ImmutableList<Move> JumpsOn(Position alone, PieceKind kind)
    {
        ImmutableList<Direction> lines =
        [
            .. kind.Movement
                .Where(rule => rule.IsJumping)
                .SelectMany(rule => Grid.Resolve(rule.Directions, Facing.Forward))
                .Distinct(),
        ];
        Position victims = lines
            .SelectMany(line => ((int[])[1, 3]).Select(distance => Grid.CellAt(
                Middle + (distance * line.First),
                Middle + (distance * line.Second)
            )))
            .Aggregate(alone, (position, cell) => position.Place(cell, new Piece(new PieceId(5), Stand, Enemy)));
        return lines.IsEmpty ? [] : [.. this.MovesOn(victims).Where(move => !move.Jumped.IsEmpty)];
    }

    /// <summary>Whether the piece may capture on <paramref name="cell"/>, the only enemy put there.</summary>
    private bool MayCapture(Position alone, CellId cell) =>
        this.MovesOn(alone.Place(cell, new Piece(new PieceId(2), Stand, Enemy)))
            .Any(move => move.Captured is not null);

    private ImmutableList<Move> MovesOn(Position position) =>
        this._moves.MovesFrom(position, Centre, Facing, piece => piece.Owner == Enemy);
}
