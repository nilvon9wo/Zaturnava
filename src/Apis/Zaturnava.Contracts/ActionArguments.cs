namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The arguments a client may read in an <see cref="ActionDto"/> without knowing its kind: who acts;
/// for an action drawn on the board, the cells it goes from and to; for an action taken at one cell,
/// as a promotion is, that cell; for a promotion, the captured piece it brings back, by id; and for an
/// action that brings a piece back onto the board once it is chosen (<see cref="ActionDto.Choice"/>), as an
/// escape does, the cell it comes back to. Every other argument is the ruleset's own, to be echoed back untouched.
/// </summary>
public static class ActionArguments
{
    public const string Actor = "actor";
    public const string From = "from";
    public const string To = "to";
    public const string Cell = "cell";
    public const string Into = "into";
    public const string Home = "home";
}
