namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>A piece asked a topology for a direction family it cannot resolve.</summary>
public sealed class UnsupportedDirectionSetException(DirectionSet set, string topology)
    : Exception($"The {topology} topology has no '{set.Name}' directions.");
