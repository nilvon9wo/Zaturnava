namespace Net.NowhereAtAll.Zaturnava.Games.GrandDraft;

/// <summary>
/// What a restriction limits (pieces.md): how the piece moves, or where it may go; or, a binding, the share of
/// the board its own move keeps it to, which nothing lifts.
/// </summary>
public enum RestrictionKind
{
    Movement,
    Area,
    Binding,
}
