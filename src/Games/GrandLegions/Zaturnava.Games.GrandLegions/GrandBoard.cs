using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The board as a game is set (specification §2): its size, its kingdoms' size and facing, and its rivers.
/// The four seats go clockwise from the bottom-left corner, as in Chaturaji 2027: bottom-left, top-left,
/// top-right, bottom-right. A kingdom is <paramref name="KingdomWidth"/> along its back rank and
/// <paramref name="KingdomDepth"/> from its back rank forward. A river lies along the line before column
/// (or row) half the side, rounded down, so on an odd side the eastern (or northern) part is a column larger.
/// </summary>
public sealed record GrandBoard(
    int Width,
    int Length,
    int KingdomWidth,
    int KingdomDepth,
    Facing Facing = Facing.Pinwheel,
    Rivers Rivers = Rivers.None
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
    /// <paramref name="kingdom"/>'s corner; across either, where there are two.
    /// </summary>
    public bool IsAcrossRiver(Kingdom kingdom, int column, int row) =>
        (this.Rivers.HasFlag(Rivers.NorthSouth) && this.IsWestHalf(column) != kingdom.IsWest)
        || (this.Rivers.HasFlag(Rivers.EastWest) && this.IsSouthHalf(row) != kingdom.IsSouth);

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

    private bool IsWestHalf(int column) => column < this.Width / 2;

    private bool IsSouthHalf(int row) => row < this.Length / 2;
}
