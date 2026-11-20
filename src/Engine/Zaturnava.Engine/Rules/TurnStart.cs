namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// How a turn began: its number; the board, the prisoners and the seat to play, as a key (equal positions have equal
/// keys); and the turns passed in a row before it.
/// </summary>
public sealed record TurnStart(int TurnNumber, long Key, int IdlePasses);
