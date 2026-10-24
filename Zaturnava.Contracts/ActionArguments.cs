namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The arguments a client may read in an <see cref="ActionDto"/> without knowing its kind: who acts,
/// and, for an action drawn on the board, the cells it goes from and to. Every other argument is the
/// ruleset's own, to be echoed back untouched.
/// </summary>
public static class ActionArguments
{
    public const string Actor = "actor";
    public const string From = "from";
    public const string To = "to";
}
