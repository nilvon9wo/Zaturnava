using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What a game played by these rules is played with (Proposed): its board, <paramref name="Side"/> cells square,
/// with four seats in a pinwheel; its pieces, how they move, what taking each scores, and their names; which
/// kinds lead an army - a leader is what Chaturaji 2027 calls a Raja, and its start is its seat's throne - and
/// which are Pawns, promoted at the far edge, swapping back to back; and where every piece starts.
/// <see cref="Standard"/> is Chaturaji 2027's own; Grand Legions plays others.
/// </summary>
public sealed record Chaturaji2027Variant(
    string Id,
    string Name,
    int Side,
    ImmutableDictionary<PieceKindId, PieceKind> Kinds,
    ImmutableDictionary<PieceKindId, int> Points,
    ImmutableDictionary<PieceKindId, string> Names,
    ImmutableHashSet<PieceKindId> Leaders,
    ImmutableHashSet<PieceKindId> Pawns,
    ImmutableList<Placement> Start
)
{
    public static Chaturaji2027Variant Standard { get; } = new(
        Chaturaji2027Rules.RulesetId,
        "Chaturaji 2027",
        Chaturaji2027Setup.Side,
        Chaturaji2027Pieces.Kinds,
        Chaturaji2027Pieces.Points,
        Chaturaji2027Pieces.ChaturajiNames,
        [Chaturaji2027Pieces.Raja],
        [Chaturaji2027Pieces.Pawn],
        Chaturaji2027Setup.Start
    )
    {
        Heavies = [Chaturaji2027Pieces.Elephant],
    };

    /// <summary>
    /// The game this is one size of, if it is played at several; none, for a game of one size, which is its own.
    /// </summary>
    public GameSize? Game { get; init; }

    /// <summary>
    /// The kinds promoted, at the far edge, into a captured piece (rules.md §8): the <see cref="Pawns"/>, unless set.
    /// </summary>
    public ImmutableHashSet<PieceKindId> Promotables
    {
        get => field ?? this.Pawns;
        init;
    }

    /// <summary>Each kind's own promotion, as a Western Pawn's into a Queen: none, unless set.</summary>
    public ImmutableDictionary<PieceKindId, OwnPromotion> OwnPromotions { get; init; } = [];

    /// <summary>The kinds, leaders apart, most dangerous to let loose, as a Rook is: none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> Heavies { get; init; } = [];

    /// <summary>How each kind is marked on its piece, where it is not drawn as itself: none, unless set.</summary>
    public ImmutableDictionary<PieceKindId, PieceMark> Marks { get; init; } = [];

    /// <summary>
    /// What a player may want told of each kind beyond its moves, as its class and what holds it back: none, unless
    /// set. The rules add the powers they give it.
    /// </summary>
    public ImmutableDictionary<PieceKindId, PieceGuide> Guides { get; init; } = [];

    /// <summary>What the bonuses score: Chaturaji 2027's own, unless set.</summary>
    public Scoring Scoring { get; init; } = Scoring.Standard;

    /// <summary>What a die showing each value is called, where not by the kinds it lets move: none.</summary>
    public ImmutableDictionary<int, string> DieNames { get; init; } = [];

    /// <summary>
    /// Whether a piece may make a move its kind allows, as the board has it (a river, say); null, if every
    /// such move may be made.
    /// </summary>
    public MoveAllowing? MayMove { get; init; }

    /// <summary>
    /// The kinds with start-row powers, the two-cell first move and <i>en passant</i>, if they start within their
    /// army's <see cref="StartRows"/>: none, unless set.
    /// </summary>
    public ImmutableHashSet<PieceKindId> StartRowPowers { get; init; } = [];

    /// <summary>How many of an army's rows, from its back rank, are its start rows: 2, unless set.</summary>
    public int StartRows { get; init; } = 2;

    /// <summary>The kinds that may castle (a Western King): none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> Castlers { get; init; } = [];

    /// <summary>The kinds a castler may castle with (a Western Rook): none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> CastlingRooks { get; init; } = [];

    /// <summary>The kinds with the flying-general capture (a Xiangqi General): none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> FlyingCapturers { get; init; } = [];

    /// <summary>The kinds that may call a leader face-off (a Janggi General): none, unless set.</summary>
    public ImmutableHashSet<PieceKindId> FaceOffCallers { get; init; } = [];

    /// <summary>
    /// Where every piece of a game dealt by a seed starts, each game its own armies - each seat's the same, if asked
    /// for alike - on the terrain dealt it (<see cref="TerrainDealer"/>); null, if every game starts as
    /// <see cref="Start"/> has it.
    /// </summary>
    public Func<int, bool, int, ImmutableList<Placement>>? Dealer { get; init; }

    /// <summary>
    /// The terrain a game dealt by a seed is dealt (<see cref="GameState.Terrain"/>), as the variant numbers its
    /// terrains; null, if every game is played on the board as it is.
    /// </summary>
    public Func<int, int>? TerrainDealer { get; init; }

    /// <summary>The lines drawn on the board, as its rivers, in the game as it stands; none, if null.</summary>
    public Func<GameState, SquareGridTopology, ImmutableList<BoardLine>>? Lines { get; init; }

    /// <summary>What a player should be told of the terrain in the game as it stands; nothing, if null.</summary>
    public Func<GameState, string?>? Terrain { get; init; }

    /// <summary>Each seat's throne: the square its first leader starts on.</summary>
    public ImmutableDictionary<SeatId, CellId> Thrones([Required] SquareGridTopology grid) =>
        this.Start
            .Where(placed => this.Leaders.Contains(placed.Kind))
            .GroupBy(placed => placed.Owner)
            .ToImmutableDictionary(army => army.Key, army => grid.CellAt(army.First().Column, army.First().Row));

    public Position InitialPosition([Required] SquareGridTopology grid) => Pinwheel.PositionOf(grid, this.Start);
}
