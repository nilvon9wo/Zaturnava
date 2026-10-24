namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A cell of the board, where to draw it (in board units) and its shape: a client draws any board
/// from these alone, whatever its topology.
/// </summary>
public sealed record CellDto(int Id, double X, double Y, string Shape);
