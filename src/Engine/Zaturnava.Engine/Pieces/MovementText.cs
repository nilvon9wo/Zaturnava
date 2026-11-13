using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// A movement rule told for people: "Any distance orthogonally, up to the first piece in the way". Whether it moves,
/// captures or both is left to whoever lists it under one or the other.
/// </summary>
public static class MovementText
{
    private const string OverAnything = ", over anything between";

    /// <summary>The rule, naming the kinds it shuns as <paramref name="nameOf"/> does.</summary>
    public static string Of([Required] MovementRule rule, [Required] Func<PieceKindId, string> nameOf) =>
        (rule.IsHopping, rule.IsLeaping) switch
        {
            _ when rule.IsJumping => $"Jumps {DirectionOf(rule.Directions)} over a piece next to it, taking it, to the "
                + "empty cell just beyond; and on from there, as often as it can, or stops",
            (true, _) => HopOf(rule, nameOf),
            (_, true) => LeapOf(rule) + (rule.IsLame ? ", unless a piece blocks the way" : OverAnything),
            _ => SlideOf(rule),
        };

    /// <summary>
    /// "Hops orthogonally over the first piece in its line, whether next to it or any number of empty cells away, to
    /// any cell beyond it up to the next piece": saying plainly that no empty cell is needed before the piece it hops,
    /// and that any number may come before it (the user's reports of 2026-11-12, of not knowing).
    /// </summary>
    private static string HopOf(MovementRule rule, Func<PieceKindId, string> nameOf)
    {
        string landing = rule.MaximumBeyond == 1
            ? "landing just beyond it"
            : "to any cell beyond it up to the next piece";
        string shunned = rule.Shunned.IsEmpty
            ? string.Empty
            : ", never over or onto a "
                + string.Join(" or ", rule.Shunned.Select(nameOf).Order(StringComparer.Ordinal));
        return $"Hops {DirectionOf(rule.Directions)} over the first piece in its line, "
            + $"whether next to it or any number of empty cells away, {landing}{shunned}";
    }

    /// <summary>"Leaps as a Knight: two cells one way and one square to it".</summary>
    private static string LeapOf(MovementRule rule) =>
        (rule.Directions == DirectionSets.Knight, LeaperOf(rule.Directions)) switch
        {
            (true, _) => "Leaps as a Knight: two cells one way and one square to it",
            (_, ({ } first, 0)) => $"Leaps exactly {first} cells orthogonally",
            (_, ({ } first, { } second)) when first == second => $"Leaps exactly {first} cells diagonally",
            (_, ({ } first, { } second)) => $"Leaps {first} cells one way and {second} square to it",
            _ => $"Leaps exactly {rule.MinimumDistance} cells {DirectionOf(rule.Directions)}",
        };

    /// <summary>"One cell diagonally", "Any distance in any direction, up to the first piece in the way".</summary>
    private static string SlideOf(MovementRule rule) =>
        (rule.Directions == DirectionSets.Knight, rule.MinimumDistance, rule.MaximumDistance) switch
        {
            (true, _, _) => "Any number of Knight's leaps in one line, up to the first piece in the way",
            (_, _, 1) => $"One cell {DirectionOf(rule.Directions)}",
            (_, 1, int.MaxValue) => $"Any distance {DirectionOf(rule.Directions)}, up to the first piece in the way",
            _ => $"From {rule.MinimumDistance} to {rule.MaximumDistance} cells {DirectionOf(rule.Directions)}, "
                + "up to the first piece in the way",
        };

    /// <summary>The two distances of a leaper's set ("leaper-3-2"); none, for any other.</summary>
    private static (int? First, int? Second) LeaperOf(DirectionSet directions) =>
        directions.Name.StartsWith(DirectionSets.LeaperPrefix, StringComparison.Ordinal)
            ? DistancesOf(directions.Name[DirectionSets.LeaperPrefix.Length..].Split('-'))
            : (null, null);

    private static (int? First, int? Second) DistancesOf(string[] distances) =>
        (int.Parse(distances[0], CultureInfo.InvariantCulture), int.Parse(distances[1], CultureInfo.InvariantCulture));

    /// <summary>"orthogonally", "straight forward"; the set's own name, for one not told otherwise.</summary>
    private static string DirectionOf(DirectionSet directions) =>
        directions.Name switch
        {
            "orthogonal" => "orthogonally",
            "diagonal" => "diagonally",
            "any" => "in any direction",
            "forward" => "straight forward",
            "forward-diagonals" => "diagonally forward",
            "sideways" => "sideways",
            _ => directions.Name,
        };
}
