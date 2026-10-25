namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The arguments a client may read in an <see cref="ActionDto"/> without knowing its kind: who acts;
/// for an action drawn on the board, the cells it goes from and to; for an action taken at one cell,
/// as a promotion is, that cell; and for a promotion, the captured piece it brings back, by id. Every
/// other argument is the ruleset's own, to be echoed back untouched.
/// </summary>
public static class ActionArguments
{
    public const string Actor = "actor";
    public const string From = "from";
    public const string To = "to";
    public const string Cell = "cell";
    public const string Into = "into";
}
