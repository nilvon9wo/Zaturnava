using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
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
    string Size,
    int Side,
    int KingdomWidth,
    int KingdomDepth,
    int Budget,
    ImmutableDictionary<PieceClass, int> Minimums
)
{
    public const string GameId = "grand-legions";
    public const string GameName = "Grand Legions";

    public static QuickGame Small { get; } = new(
        "grand-legions-small",
        "small",
        6,
        3,
        3,
        110,
        Counts((Leader, 1), (Heavy, 1), (Cavalry, 1), (Infantry, 3))
    );

    public static QuickGame Medium { get; } = new(
        "grand-legions-medium",
        "medium",
        8,
        4,
        2,
        125,
        Counts((Leader, 1), (Heavy, 1), (Cavalry, 1), (Artillery, 1), (Infantry, 4))
    );

    public static QuickGame Large { get; } = new(
        "grand-legions-large",
        "large",
        12,
        6,
        3,
        200,
        Counts((Leader, 1), (Heavy, 3), (Cavalry, 2), (Artillery, 2), (Infantry, 8))
    );

    public static ImmutableList<QuickGame> All { get; } = [Small, Medium, Large];

    /// <summary>The game and its size, as the rules tell them.</summary>
    public GameSize Game => new(GameId, GameName, this.Size);

    /// <summary>What a game of this size is called: "Grand Legions (small)".</summary>
    public string Name => $"{GameName} ({this.Size})";

    /// <summary>How many cells each kingdom has, for an army to start on.</summary>
    public int Cells => this.KingdomWidth * this.KingdomDepth;

    /// <summary>
    /// Whether neutral ground lies between neighbouring kingdoms: as the kingdoms turn about the board, one's back
    /// rank and its neighbour's depth together fall short of the side. Where they do not, the kingdoms touch.
    /// </summary>
    public bool HasNeutralZone => this.KingdomWidth + this.KingdomDepth < this.Side;

    private static ImmutableDictionary<PieceClass, int> Counts(params (PieceClass Class, int Count)[] counts) =>
        counts.ToImmutableDictionary(count => count.Class, count => count.Count);
}
