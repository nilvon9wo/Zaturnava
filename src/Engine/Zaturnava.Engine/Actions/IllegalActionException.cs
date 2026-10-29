namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>A well-formed action that the rules do not allow now; its message is fit to show a player.</summary>
public sealed class IllegalActionException(GameAction action)
    : Exception($"That {action.Describe().Kind} is not allowed now.");
