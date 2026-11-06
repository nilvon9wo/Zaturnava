namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A line drawn on the board, in board units as the cells are (<see cref="CellDto"/>), from (<paramref name="FromX"/>,
/// <paramref name="FromY"/>) to (<paramref name="ToX"/>, <paramref name="ToY"/>), of a <paramref name="Kind"/> a
/// client draws its own way: "river".
/// </summary>
public sealed record LineDto(string Kind, double FromX, double FromY, double ToX, double ToY);
