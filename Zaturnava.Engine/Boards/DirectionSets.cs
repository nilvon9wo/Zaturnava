namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>The direction families the movement grammar refers to.</summary>
public static class DirectionSets
{
    public static readonly DirectionSet Orthogonal = new("orthogonal");
    public static readonly DirectionSet Diagonal = new("diagonal");
    public static readonly DirectionSet Knight = new("knight");
    public static readonly DirectionSet Forward = new("forward");
    public static readonly DirectionSet ForwardDiagonals = new("forward-diagonals");
}
