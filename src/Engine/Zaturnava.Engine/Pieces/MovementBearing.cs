using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Which way a piece goes, at a glance, for people sorting pieces (Proposed): by the lines it steps, slides or hops
/// along, its leaps aside. Only forward (a Pawn, a Lance); only orthogonally (a Rook); only diagonally (a Bishop);
/// every way (a Queen, a King); else, as a piece with only leaps, or lines of both sorts but not all, other.
/// </summary>
public static class MovementBearing
{
    public const string Forward = "Forward";
    public const string Orthogonal = "Orthogonal";
    public const string Diagonal = "Diagonal";
    public const string EveryWay = "Every way";
    public const string Other = "Other";

    /// <summary>Every bearing, in the order people are offered them.</summary>
    public static ImmutableList<string> All { get; } = [Forward, Orthogonal, Diagonal, EveryWay, Other];

    private static readonly ImmutableHashSet<DirectionSet> Ahead =
        [DirectionSets.Forward, DirectionSets.ForwardDiagonals];

    public static string Of([Required] IEnumerable<MovementRule> movement)
    {
        ImmutableHashSet<DirectionSet> lines =
        [
            .. movement.Select(rule => rule.Directions)
                .Where(set =>
                    set != DirectionSets.Knight
                    && !set.Name.StartsWith(DirectionSets.LeaperPrefix, StringComparison.Ordinal)
                ),
        ];
        bool isOrthogonal = lines.Contains(DirectionSets.Orthogonal);
        bool isDiagonal = lines.Contains(DirectionSets.Diagonal);
        return lines switch
        {
            { IsEmpty: true } => Other,
            _ when lines.Contains(DirectionSets.Any) || (isOrthogonal && isDiagonal) => EveryWay,
            _ when lines.IsSubsetOf(Ahead) => Forward,
            _ when lines.SetEquals([DirectionSets.Orthogonal]) => Orthogonal,
            _ when lines.SetEquals([DirectionSets.Diagonal]) => Diagonal,
            _ => Other,
        };
    }
}
