namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A cell of the board, where to draw it (in board units), its shape and its name: a client draws
/// any board from these alone, whatever its topology. <see cref="Throne"/> is the seat whose throne
/// it is, if any.
/// </summary>
public sealed record CellDto(int Id, double X, double Y, string Shape, int? Throne, string Name);
