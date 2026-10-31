using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Engine.Boards;

/// <summary>The direction families the movement grammar refers to.</summary>
public static class DirectionSets
{
    /// <summary>What every <see cref="Leaper"/> set's name begins with, followed by its two distances.</summary>
    public const string LeaperPrefix = "leaper-";

    public static readonly DirectionSet Orthogonal = new("orthogonal");
    public static readonly DirectionSet Diagonal = new("diagonal");
    public static readonly DirectionSet Knight = new("knight");
    public static readonly DirectionSet Forward = new("forward");
    public static readonly DirectionSet ForwardDiagonals = new("forward-diagonals");

    /// <summary>The two directions square to forward: left and right.</summary>
    public static readonly DirectionSet Sideways = new("sideways");

    /// <summary>The orthogonal and diagonal directions together: every line a Queen moves along.</summary>
    public static readonly DirectionSet Any = new("any");

    /// <summary>
    /// The leaps of <paramref name="first"/> cells one way and <paramref name="second"/> square to it, every
    /// way round: the Knight's are (2, 1), the Zebra's (3, 2).
    /// </summary>
    public static DirectionSet Leaper([Range(0, int.MaxValue)] int first, [Range(0, int.MaxValue)] int second) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{LeaperPrefix}{first}-{second}"));
}
