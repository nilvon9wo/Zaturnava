using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A seat's kingdom (specification §2): the rectangle of cells in its corner where its army starts, from
/// (<paramref name="Column"/>, <paramref name="Row"/>), row 0 at the bottom, as the board lies it; and its
/// army's forward, away from its back rank.
/// </summary>
public sealed record Kingdom(
    SeatId Seat,
    int Column,
    int Row,
    int Width,
    int Height,
    Direction Forward,
    bool IsWest,
    bool IsSouth
)
{
    public bool Contains(int column, int row) =>
        column >= this.Column
        && column < this.Column + this.Width
        && row >= this.Row
        && row < this.Row + this.Height;

    public bool Overlaps(Kingdom other) =>
        this.Column < other.Column + other.Width
        && other.Column < this.Column + this.Width
        && this.Row < other.Row + other.Height
        && other.Row < this.Row + this.Height;
}
