namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A direction on the board, as its cells' layouts lay it out (<see cref="CellDto.X"/>, <see cref="CellDto.Y"/>):
/// <paramref name="X"/> to the right, <paramref name="Y"/> up, row 0 at the bottom.
/// </summary>
public sealed record DirectionDto(int X, int Y);
