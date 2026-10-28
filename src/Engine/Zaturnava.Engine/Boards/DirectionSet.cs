namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>
/// A named family of directions ("orthogonal", "forward"), which each topology resolves into the
/// concrete directions it supports, relative to a seat's forward direction where that matters.
/// </summary>
public sealed record DirectionSet(string Name);
