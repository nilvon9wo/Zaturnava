namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>
/// Where a cell is drawn, in board units, and its shape: enough for any client to render any
/// topology without knowing which one it is.
/// </summary>
public sealed record CellLayout(double X, double Y, string Shape);
