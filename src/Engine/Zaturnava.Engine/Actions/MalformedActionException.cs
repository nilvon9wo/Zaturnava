namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>A descriptor that does not describe any action.</summary>
public sealed class MalformedActionException(string message) : Exception(message);
