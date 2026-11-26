namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Which of a piece's restrictions a lift removes (specification §4): one on how the piece moves (a Horse's leg that
/// may be blocked), or one on where it may go (its palace, its side of a river). Each is priced apart.
/// </summary>
public enum LiftKind
{
    Movement,
    Area,
}
