using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Tradition;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The catalogue of pieces (docs/grand/pieces.md), most valuable first within each class. Each moves as its
/// own game has it; what its area restrictions (palace, river) and powers (castling, the flying general...)
/// add is the ruleset's, still to come. Keys are kept apart from Chaturaji 2027's, whose Horse and Elephant
/// move otherwise.
/// </summary>
public static class GrandLegionsPieces
{
    private const string Palace = "Only its 3 × 3 palace";
    private const string BlockedAhead = "Blocked straight ahead";
    private const string OneColour = "One colour of square";
    private const string NeverBack = "Never back";

    public static readonly PieceKindId DragonKing = new("dragon-king");
    public static readonly PieceKindId FairyGeneral = new("fairy-general");
    public static readonly PieceKindId WesternKing = new("western-king");
    public static readonly PieceKindId XiangqiGeneral = new("xiangqi-general");
    public static readonly PieceKindId JanggiGeneral = new("janggi-general");
    public static readonly PieceKindId GoldGeneral = new("gold-general");
    public static readonly PieceKindId SilverGeneral = new("silver-general");

    public static readonly PieceKindId ShogiPawn = new("shogi-pawn");
    public static readonly PieceKindId WesternPawn = new("western-pawn");
    public static readonly PieceKindId JanggiGuard = new("janggi-guard");
    public static readonly PieceKindId Noble = new("noble");
    public static readonly PieceKindId JanggiSoldier = new("janggi-soldier");
    public static readonly PieceKindId Minister = new("minister");
    public static readonly PieceKindId Courtier = new("courtier");
    public static readonly PieceKindId MakrukPawn = new("makruk-pawn");
    public static readonly PieceKindId XiangqiSoldier = new("xiangqi-soldier");
    public static readonly PieceKindId XiangqiAdvisor = new("xiangqi-advisor");

    public static readonly PieceKindId Amazon = new("amazon");
    public static readonly PieceKindId Queen = new("queen");
    public static readonly PieceKindId Chancellor = new("chancellor");
    public static readonly PieceKindId Archbishop = new("archbishop");
    public static readonly PieceKindId DragonBishop = new("dragon-bishop");
    public static readonly PieceKindId DragonHorse = new("dragon-horse");
    public static readonly PieceKindId Lance = new("lance");
    public static readonly PieceKindId Rook = new("rook");
    public static readonly PieceKindId Bishop = new("bishop");

    public static readonly PieceKindId Knightrider = new("knightrider");
    public static readonly PieceKindId Zebra = new("zebra");
    public static readonly PieceKindId Knight = new("knight");
    public static readonly PieceKindId Camel = new("camel");
    public static readonly PieceKindId Horse = new("xiangqi-horse");
    public static readonly PieceKindId JanggiElephant = new("janggi-elephant");
    public static readonly PieceKindId Giraffe = new("giraffe");
    public static readonly PieceKindId Alfil = new("alfil");
    public static readonly PieceKindId Threeleaper = new("threeleaper");
    public static readonly PieceKindId XiangqiElephant = new("xiangqi-elephant");
    public static readonly PieceKindId WarEngine = new("war-engine");

    public static readonly PieceKindId XiangqiCannon = new("xiangqi-cannon");
    public static readonly PieceKindId JanggiCannon = new("janggi-cannon");
    public static readonly PieceKindId Grasshopper = new("grasshopper");

    private static readonly MovementRule KnightLeap = MovementRule.Leap(DirectionSets.Knight, 1);
    private static readonly MovementRule OrthogonalStep = MovementRule.Step(DirectionSets.Orthogonal);
    private static readonly MovementRule DiagonalStep = MovementRule.Step(DirectionSets.Diagonal);
    private static readonly MovementRule ForwardStep = MovementRule.Step(DirectionSets.Forward);
    private static readonly MovementRule OrthogonalSlide = MovementRule.Slide(DirectionSets.Orthogonal);
    private static readonly MovementRule DiagonalSlide = MovementRule.Slide(DirectionSets.Diagonal);

    public static readonly ImmutableList<GrandPiece> Catalogue =
    [
        // Leaders
        Piece(DragonKing, "Dragon King (Shogi)", Leader, Shogi, 78, OrthogonalSlide, DiagonalStep),
        Piece(FairyGeneral, "Fairy General", Leader, Fairy, 76, Step(DirectionSets.Any), KnightLeap),
        Piece(WesternKing, "King (Western)", Leader, Western, 70, Step(DirectionSets.Any)),
        Piece(XiangqiGeneral, "General (Xiangqi)", Leader, Xiangqi, 69, OrthogonalStep)
            .Restricted(Area(Palace, 5)),
        Piece(JanggiGeneral, "General (Janggi)", Leader, Janggi, 68, OrthogonalStep)
            .Restricted(Area(Palace, 5)),
        Piece(
            GoldGeneral,
            "Gold General (Shogi)",
            Leader,
            Shogi,
            66,
            OrthogonalStep,
            Step(DirectionSets.ForwardDiagonals)
        ),
        Piece(SilverGeneral, "Silver General (Shogi)", Leader, Shogi, 65, DiagonalStep, ForwardStep)
            .Promoting(GoldGeneral),

        // Infantry
        Piece(ShogiPawn, "Pawn (Shogi)", Infantry, Shogi, 17, ForwardStep)
            .Restricted(Moves(BlockedAhead, 2))
            .Promoting(GoldGeneral),
        Piece(WesternPawn, "Pawn (Western)", Infantry, Western, 7, PawnMovement())
            .Restricted(Moves(BlockedAhead, 2))
            .Promoting(Queen, Rook, Bishop, Knight),
        Piece(JanggiGuard, "Guard (Janggi)", Infantry, Janggi, 7, OrthogonalStep)
            .Restricted(Area(Palace, 5)),
        Piece(Noble, "Noble (Makruk)", Infantry, Makruk, 7, DiagonalStep, ForwardStep),
        Piece(JanggiSoldier, "Soldier (Janggi)", Infantry, Janggi, 7, ForwardStep, Sideways())
            .Restricted(
                Moves(NeverBack, 2),
                Area("Diagonally only along palace lines", 2)
            ),
        Piece(Minister, "Minister", Infantry, Fairy, 6, OrthogonalStep),
        Piece(Courtier, "Courtier", Infantry, Fairy, 5, DiagonalStep)
            .Restricted(Bound(OneColour)),
        Piece(MakrukPawn, "Pawn (Makruk)", Infantry, Makruk, 4, PawnMovement())
            .Restricted(Moves(BlockedAhead, 2))
            .Promoting(Courtier),
        Piece(XiangqiSoldier, "Soldier (Xiangqi)", Infantry, Xiangqi, 4, ForwardStep)
            .Restricted(
                Moves(NeverBack, 2),
                Area("Sideways only once across a river", 3)
            ),
        Piece(XiangqiAdvisor, "Advisor (Xiangqi)", Infantry, Xiangqi, 2, DiagonalStep)
            .Restricted(Area(Palace, 5)),

        // Heavy / Slider
        Piece(Amazon, "Amazon", Heavy, Fairy, 37, MovementRule.Slide(DirectionSets.Any), KnightLeap),
        Piece(Queen, "Queen (Western)", Heavy, Western, 27, MovementRule.Slide(DirectionSets.Any)),
        Piece(Chancellor, "Chancellor", Heavy, Fairy, 22, OrthogonalSlide, KnightLeap),
        Piece(Archbishop, "Archbishop", Heavy, Fairy, 21, DiagonalSlide, KnightLeap),
        Piece(DragonBishop, "Dragon Bishop", Heavy, Fairy, 21, DiagonalSlide, KnightLeap),
        Piece(DragonHorse, "Dragon Horse (Shogi)", Heavy, Shogi, 17, DiagonalSlide, OrthogonalStep),
        Piece(Lance, "Lance (Shogi)", Heavy, Shogi, 16, MovementRule.Slide(DirectionSets.Forward))
            .Restricted(Moves("Blocked by pieces in its path", 2))
            .Promoting(GoldGeneral),
        Piece(Rook, "Rook (Western)", Heavy, Western, 14, OrthogonalSlide),
        Piece(Bishop, "Bishop (Western)", Heavy, Western, 12, DiagonalSlide)
            .Restricted(Bound(OneColour)),

        // Cavalry / Leaper
        Piece(Knightrider, "Knightrider", Cavalry, Fairy, 12, MovementRule.Slide(DirectionSets.Knight))
            .Restricted(Moves("Each landing short of the last must be empty", 3)),
        Piece(Zebra, "Zebra", Cavalry, Fairy, 8, Leaper(3, 2)),
        Piece(Knight, "Knight (Western)", Cavalry, Western, 8, KnightLeap),
        Piece(Camel, "Camel", Cavalry, Fairy, 7, Leaper(3, 1))
            .Restricted(Bound(OneColour)),
        Piece(Horse, "Horse (Xiangqi)", Cavalry, Xiangqi, 7, KnightLeap.Lame())
            .Restricted(Moves("The first square of its leg must be empty", 2)),
        Piece(JanggiElephant, "Elephant (Janggi)", Cavalry, Janggi, 7, Leaper(3, 2).Lame())
            .Restricted(Moves("Each cell on the way must be empty", 2)),
        Piece(Giraffe, "Giraffe", Cavalry, Fairy, 4, Leaper(4, 1)),
        Piece(Alfil, "Elephant (Alfil)", Cavalry, Fairy, 2, Leaper(2, 2))
            .Restricted(Bound("One cell in eight")),
        Piece(Threeleaper, "Threeleaper", Cavalry, Fairy, 2, Leaper(3, 0))
            .Restricted(Bound("One cell in nine")),
        Piece(XiangqiElephant, "Elephant (Xiangqi)", Cavalry, Xiangqi, 2, Leaper(2, 2).Lame())
            .Restricted(
                Moves("The square between must be empty", 2),
                Area("May not cross a river", 3),
                Bound("One cell in eight")
            ),
        Piece(WarEngine, "War Engine", Cavalry, Fairy, 2, Leaper(2, 0))
            .Restricted(Bound("One cell in four")),

        // Specialty / Artillery
        Piece(
            XiangqiCannon,
            "Cannon (Xiangqi)",
            Artillery,
            Xiangqi,
            10,
            OrthogonalSlide.MoveOnly(),
            MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).CaptureOnly()
        ),
        Piece(
                JanggiCannon,
                "Cannon (Janggi)",
                Artillery,
                Janggi,
                8,
                MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).Shunning([XiangqiCannon, JanggiCannon])
            )
            .Restricted(Moves("Must hop exactly one piece, and never a Cannon", 3)),
        Piece(Grasshopper, "Grasshopper", Artillery, Fairy, 7, MovementRule.Hop(DirectionSets.Any, 1))
            .Restricted(Moves("Must hop, landing just beyond", 3)),
    ];

    /// <summary>Every piece of the catalogue, by its key.</summary>
    public static readonly ImmutableDictionary<PieceKindId, GrandPiece> ById =
        Catalogue.ToImmutableDictionary(piece => piece.Id);

    /// <summary>How every piece of the catalogue moves, by its key, for a <c>MoveGenerator</c>.</summary>
    public static readonly ImmutableDictionary<PieceKindId, PieceKind> Kinds =
        Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Kind);

    private static GrandPiece Piece(
        PieceKindId id,
        string name,
        PieceClass pieceClass,
        Tradition tradition,
        int value,
        params MovementRule[] movement
    ) =>
        new(id, name, pieceClass, tradition, value, [.. movement]);

    /// <summary>A restriction on how the piece moves, lifted for <paramref name="lift"/>.</summary>
    private static Restriction Moves(string description, int lift) => new(RestrictionKind.Movement, description, lift);

    /// <summary>A restriction on where the piece may go, lifted for <paramref name="lift"/>.</summary>
    private static Restriction Area(string description, int lift) => new(RestrictionKind.Area, description, lift);

    /// <summary>The share of the board the piece's own move keeps it to: nothing lifts it.</summary>
    private static Restriction Bound(string description) => new(RestrictionKind.Binding, description);

    private static MovementRule Step(DirectionSet directions) => MovementRule.Step(directions);

    private static MovementRule Sideways() => MovementRule.Step(DirectionSets.Sideways);

    private static MovementRule Leaper(int first, int second) =>
        MovementRule.Leap(DirectionSets.Leaper(first, second), 1);

    private static MovementRule[] PawnMovement() =>
    [
        MovementRule.Step(DirectionSets.Forward).MoveOnly(),
        MovementRule.Step(DirectionSets.ForwardDiagonals).CaptureOnly(),
    ];
}
