namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>
/// One step on a board, as the topology that resolves it understands it: a square grid reads it as
/// (columns, rows), a hex grid could read it as axial (q, r). Pieces never interpret it themselves.
/// </summary>
public readonly record struct Direction(int First, int Second);
