using System.Collections.Immutable;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A quick game's size (as the game's author set them): a square board <paramref name="Side"/> cells across, each
/// kingdom <paramref name="KingdomWidth"/> along its back rank and <paramref name="KingdomDepth"/> deep, and each
/// army's budget, spent on at least <paramref name="Minimums"/> of each class. The program picks the armies and
/// lays them out.
/// </summary>
public sealed record QuickGame(
    string Id,
    string Name,
    int Side,
    int KingdomWidth,
    int KingdomDepth,
    int Budget,
    ImmutableDictionary<PieceClass, int> Minimums
)
{
    public static QuickGame Small { get; } = new(
        "grand-legions-small",
        "Grand Legions (small)",
        6,
        3,
        3,
        110,
        Counts((Leader, 1), (Heavy, 1), (Cavalry, 1), (Infantry, 3))
    );

    public static QuickGame Medium { get; } = new(
        "grand-legions-medium",
        "Grand Legions (medium)",
        8,
        4,
        2,
        125,
        Counts((Leader, 1), (Heavy, 1), (Cavalry, 1), (Artillery, 1), (Infantry, 4))
    );

    public static QuickGame Large { get; } = new(
        "grand-legions-large",
        "Grand Legions (large)",
        12,
        6,
        3,
        200,
        Counts((Leader, 1), (Heavy, 3), (Cavalry, 2), (Artillery, 2), (Infantry, 8))
    );

    public static ImmutableList<QuickGame> All { get; } = [Small, Medium, Large];

    /// <summary>How many cells each kingdom has, for an army to start on.</summary>
    public int Cells => this.KingdomWidth * this.KingdomDepth;

    private static ImmutableDictionary<PieceClass, int> Counts(params (PieceClass Class, int Count)[] counts) =>
        counts.ToImmutableDictionary(count => count.Class, count => count.Count);
}
