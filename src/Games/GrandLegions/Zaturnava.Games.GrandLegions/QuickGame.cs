using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A quick game's size (as the game's author set them): a board <paramref name="Width"/> cells across and
/// <see cref="Height"/> up, each kingdom <paramref name="KingdomWidth"/> along its back rank and
/// <paramref name="KingdomDepth"/> deep, and each army's budget, spent on at least <paramref name="Minimums"/> of each
/// class. The program picks the armies and lays them out.
/// </summary>
public sealed record QuickGame(
    string Id,
    string Size,
    int Width,
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

    /// <summary>
    /// What a custom game's creator spends on each army for each cell of its kingdom, unless they set another budget
    /// (Proposed, 2026-11-12: about what the small and large games give, 110 for 9 cells and 200 for 18).
    /// </summary>
    public const int BudgetPerKingdomCell = 12;

    /// <summary>The size of a game whose board its creator sets (<see cref="CustomOf"/>).</summary>
    public const string CustomSize = "custom";

    /// <summary>
    /// A game whose board and kingdoms its creator sets (Stated, 2026-11-12), as offered before they set them
    /// (Proposed): a 10 × 10 board, with kingdoms 4 × 3.
    /// </summary>
    public static QuickGame Custom { get; } = CustomOf(new BoardSize(10, 10, 4, 3))! with { IsSet = false };

    /// <summary>
    /// How many cells up the board is: as many as across, a square, unless its creator set another (Stated
    /// 2026-11-13: boards need not be square).
    /// </summary>
    public int Height
    {
        get => field > 0 ? field : this.Width;
        init;
    }

    public static ImmutableList<QuickGame> All { get; } = [Small, Medium, Large, Custom];

    /// <summary>
    /// Whether the game is a preview (Stated: until its own pieces' symbols, thrones and Elephants are done).
    /// </summary>
    public const bool IsPreview = true;

    /// <summary>
    /// The rivers across the board its armies are picked for: none, unless a game is dealt rivers
    /// (<see cref="QuickTerrain"/>).
    /// </summary>
    public Rivers Rivers { get; init; } = Rivers.None;

    /// <summary>
    /// Whether the armies picked for it may hold the pieces a palace holds, in their palace: not unless a game is
    /// dealt them (<see cref="ArmyDealing.HasPalacePieces"/>).
    /// </summary>
    public bool HasPalacePieces { get; init; }

    /// <summary>
    /// Whether, its kingdom too small for a palace, its palace stands partly outside it: not unless a game is dealt
    /// so (<see cref="ArmyDealing.HasEdgePalaces"/>).
    /// </summary>
    public bool HasEdgePalaces { get; init; }

    /// <summary>
    /// The edition of the catalogue its armies are picked from (<see cref="GrandPiece.Edition"/>): the first, 0,
    /// unless a game is dealt another (<see cref="ArmyDealing.Edition"/>).
    /// </summary>
    public int Edition { get; init; }

    /// <summary>
    /// Every square of its kingdom, as seat 0 sees it, from the bottom-left corner, row by row from the back.
    /// </summary>
    public ImmutableList<ArmySquare> Kingdom =>
    [
        .. Enumerable.Range(0, this.KingdomDepth)
            .SelectMany(row => Enumerable.Range(0, this.KingdomWidth).Select(column => new ArmySquare(column, row))),
    ];

    /// <summary>Whether its creator set the board's size (<see cref="CustomOf"/>).</summary>
    public bool IsSet { get; init; }

    /// <summary>The game and its size, as the rules tell them.</summary>
    public GameSize Game => new(GameId, GameName, this.Size, IsPreview);

    /// <summary>
    /// What a game of this size is called: "Grand Legions (small)"; "Grand Legions (12 × 10, kingdoms 4 × 3)", if its
    /// creator set its size, its width first.
    /// </summary>
    public string Name => this.IsSet
        ? $"{GameName} ({this.Width} × {this.Height}, kingdoms {this.KingdomWidth} × {this.KingdomDepth})"
        : $"{GameName} ({this.Size})";

    /// <summary>The game's board, with no rivers.</summary>
    public GrandBoard Board => new(this.Width, this.Height, this.KingdomWidth, this.KingdomDepth);

    /// <summary>
    /// A custom game on a board of <paramref name="size"/>, if it may be played on it
    /// (<see cref="GrandBoard.IsPlayable"/>: no side over 26, kingdoms at least 3 × 3, none overlapping); none, if
    /// not. Each army's budget is <see cref="BudgetPerKingdomCell"/> for each cell of its kingdom; it has at least
    /// (Proposed, 2026-11-12) a leader, a front row of Infantry, a Heavy piece for each six cells, and Cavalry and
    /// Artillery for each nine, one of each at least, as the quick games have about so.
    /// </summary>
    public static QuickGame? CustomOf([Required] BoardSize size)
    {
        int cells = size.KingdomWidth * size.KingdomDepth;
        int nines = Math.Max(1, cells / 9);
        QuickGame game = new(
            QuickGame.GameId + "-" + CustomSize,
            CustomSize,
            size.Width,
            size.KingdomWidth,
            size.KingdomDepth,
            BudgetPerKingdomCell * cells,
            Counts(
                (Leader, 1),
                (Heavy, Math.Max(1, cells / 6)),
                (Cavalry, nines),
                (Artillery, nines),
                (Infantry, size.KingdomWidth)
            )
        )
        {
            Height = size.Height,
            IsSet = true,
        };
        return game.Board.IsPlayable ? game : null;
    }

    /// <summary>How many cells each kingdom has, for an army to start on.</summary>
    public int Cells => this.KingdomWidth * this.KingdomDepth;

    /// <summary>
    /// Whether neutral ground lies between every two neighbouring kingdoms: as the kingdoms turn about the board, one's
    /// back rank and its neighbour's depth together fall short of each side. Where they do not, two kingdoms touch.
    /// </summary>
    public bool HasNeutralZone => this.KingdomWidth + this.KingdomDepth < Math.Min(this.Width, this.Height);

    private static ImmutableDictionary<PieceClass, int> Counts(params (PieceClass Class, int Count)[] counts) =>
        counts.ToImmutableDictionary(count => count.Class, count => count.Count);
}
