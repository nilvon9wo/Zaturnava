using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The board as a game is set (specification §2): its size, its kingdoms' size and facing, and its rivers.
/// The four seats go clockwise from the bottom-left corner, as in Chaturaji 2027: bottom-left, top-left,
/// top-right, bottom-right. A kingdom is <paramref name="KingdomWidth"/> along its back rank and
/// <paramref name="KingdomDepth"/> from its back rank forward. A river halves the board: on an even side it runs
/// between the two middle columns (or rows) of cells; on an odd side, through the middle one, whose cells it
/// cuts in half. Where a kingdom reaches past the middle, <paramref name="Course"/> says whether the river runs
/// through it or around it.
/// </summary>
public sealed record GrandBoard(
    int Width,
    int Length,
    int KingdomWidth,
    int KingdomDepth,
    Facing Facing = Facing.Pinwheel,
    Rivers Rivers = Rivers.None,
    RiverCourse Course = RiverCourse.ThroughKingdoms
)
{
    public const int MaximumSide = 26;
    public const int MinimumKingdomSide = 3;

    private static readonly Direction North = new(0, 1);
    private static readonly Direction East = new(1, 0);
    private static readonly Direction South = new(0, -1);
    private static readonly Direction West = new(-1, 0);

    /// <summary>Each seat's kingdom, by seat.</summary>
    public ImmutableList<Kingdom> Kingdoms => this.Facing == Facing.Pinwheel
        ? this.Pinwheel()
        : this.TopAndBottom();

    /// <summary>
    /// Whether the game can be played as set: no side over 26, kingdoms at least 3 × 3 so that a palace fits,
    /// every kingdom on the board, and no two overlapping (they may touch).
    /// </summary>
    public bool IsPlayable =>
        this.Width <= MaximumSide
        && this.Length <= MaximumSide
        && this.KingdomWidth >= MinimumKingdomSide
        && this.KingdomDepth >= MinimumKingdomSide
        && this.Kingdoms.All(IsOnBoard)
        && !this.Kingdoms.SelectMany((kingdom, seat) => this.Kingdoms.Skip(seat + 1).Where(kingdom.Overlaps)).Any();

    /// <summary>
    /// Whether (<paramref name="column"/>, <paramref name="row"/>) is across a river from
    /// <paramref name="kingdom"/>'s corner; across either, where there are two. A cell the river cuts is not
    /// across it. With the river around the kingdoms, every cell of a kingdom lies on its own corner's side.
    /// </summary>
    public bool IsAcrossRiver(Kingdom kingdom, int column, int row) =>
        (this.Rivers.HasFlag(Rivers.NorthSouth) && this.WestEastOf(column, row) == -SideOf(kingdom.IsWest))
        || (this.Rivers.HasFlag(Rivers.EastWest) && this.SouthNorthOf(column, row) == -SideOf(kingdom.IsSouth));

    /// <summary>
    /// Whether a river cuts (<paramref name="column"/>, <paramref name="row"/>) in half: only on an odd side, and
    /// never in a kingdom the river goes around. A piece that may not cross a river may not enter such a cell.
    /// </summary>
    public bool IsInRiver(int column, int row) =>
        (this.Rivers.HasFlag(Rivers.NorthSouth) && this.WestEastOf(column, row) == 0)
        || (this.Rivers.HasFlag(Rivers.EastWest) && this.SouthNorthOf(column, row) == 0);

    private ImmutableList<Kingdom> Pinwheel() =>
    [
        new(new SeatId(0), 0, 0, this.KingdomWidth, this.KingdomDepth, North, true, true),
        new(
            new SeatId(1),
            0,
            this.Length - this.KingdomWidth,
            this.KingdomDepth,
            this.KingdomWidth,
            East,
            true,
            false
        ),
        new(
            new SeatId(2),
            this.Width - this.KingdomWidth,
            this.Length - this.KingdomDepth,
            this.KingdomWidth,
            this.KingdomDepth,
            South,
            false,
            false
        ),
        new(new SeatId(3), this.Width - this.KingdomDepth, 0, this.KingdomDepth, this.KingdomWidth, West, false, true),
    ];

    private ImmutableList<Kingdom> TopAndBottom()
    {
        int east = this.Width - this.KingdomWidth;
        int north = this.Length - this.KingdomDepth;
        return
        [
            new(new SeatId(0), 0, 0, this.KingdomWidth, this.KingdomDepth, North, true, true),
            new(new SeatId(1), 0, north, this.KingdomWidth, this.KingdomDepth, South, true, false),
            new(new SeatId(2), east, north, this.KingdomWidth, this.KingdomDepth, South, false, false),
            new(new SeatId(3), east, 0, this.KingdomWidth, this.KingdomDepth, North, false, true),
        ];
    }

    /// <summary>
    /// Every kingdom lies against its corner's edges, and each of its sides is matched by a kingdom against the
    /// far edges; so all are on the board when none starts before the first column or row.
    /// </summary>
    private static bool IsOnBoard(Kingdom kingdom) => Math.Min(kingdom.Column, kingdom.Row) >= 0;

    /// <summary>-1 for a side's first half (west or south), 1 for its second, 0 for an odd side's middle.</summary>
    private static int HalfOf(int index, int side) => Math.Sign((2 * index) + 1 - side);

    private static int SideOf(bool isFirstHalf) => isFirstHalf ? -1 : 1;

    private int WestEastOf(int column, int row) =>
        this.KingdomAround(column, row) is { } kingdom
            ? SideOf(kingdom.IsWest)
            : HalfOf(column, this.Width);

    private int SouthNorthOf(int column, int row) =>
        this.KingdomAround(column, row) is { } kingdom
            ? SideOf(kingdom.IsSouth)
            : HalfOf(row, this.Length);

    private Kingdom? KingdomAround(int column, int row) => this.Course == RiverCourse.AroundKingdoms
        ? this.Kingdoms.FirstOrDefault(kingdom => kingdom.Contains(column, row))
        : null;
}
