namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>
/// A line drawn on the board from (<paramref name="FromX"/>, <paramref name="FromY"/>) to (<paramref name="ToX"/>,
/// <paramref name="ToY"/>), measured as the cells' layouts are (<see cref="CellLayout"/>: each cell's centre, a unit
/// apart), of a <paramref name="Kind"/> a client draws in its own way: "river".
/// </summary>
public sealed record BoardLine(string Kind, double FromX, double FromY, double ToX, double ToY);
