namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>A well-formed action that the rules do not allow now.</summary>
public sealed class IllegalActionException(GameAction action) : Exception($"{action} is not legal now.");
