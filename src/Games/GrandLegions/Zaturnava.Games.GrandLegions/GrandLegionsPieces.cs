using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Tradition;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The catalogue of pieces (docs/grand/pieces.md), each worth what the game's table of values says. Each moves as its
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

    public static readonly PieceKindId CheckersKing = new("checkers-king");
    public static readonly PieceKindId Man = new("man");
    public static readonly PieceKindId TaflKing = new("tafl-king");
    public static readonly PieceKindId Swede = new("swede");

    public static readonly PieceKindId BerolinaPawn = new("berolina-pawn");
    public static readonly PieceKindId ManchuChariot = new("manchu-chariot");
    public static readonly PieceKindId Courier = new("courier");

    /// <summary>The edition of the catalogue the Checkers and Tafl pieces came with (2026-11-13).</summary>
    private const int CheckersAndTafl = 1;

    /// <summary>
    /// The edition of the catalogue the Berolina Pawn, the Manchu Chariot and the Courier came with (2026-11-13),
    /// after the even spread of quick armies' budgets (<see cref="QuickArmy.EvenSpreadEdition"/>).
    /// </summary>
    public const int BerolinaManchuAndCourier = 4;

    /// <summary>
    /// The edition from which the pieces that may take several pieces in one move are priced for it (2026-11-13;
    /// pieces.md, What taking several is worth): the Tafl King and the Swede, by custody, before it 71 and 11; the
    /// Checkers King and the Man, by a chain of jumps, before it 64 and 54.
    /// </summary>
    public const int MultipleCapturesRepriced = 5;

    /// <summary>
    /// What each piece is worth, its price and what taking it scores, as the game's table of values has it
    /// (<c>Values/grand-legions.json</c>), which can be changed apart from the code.
    /// </summary>
    private static readonly ImmutableDictionary<PieceKindId, int> Values =
        PieceValues.Of(typeof(GrandLegionsPieces).Assembly, QuickGame.GameId);

    private static readonly MovementRule KnightLeap = MovementRule.Leap(DirectionSets.Knight, 1);
    private static readonly MovementRule OrthogonalStep = MovementRule.Step(DirectionSets.Orthogonal);
    private static readonly MovementRule DiagonalStep = MovementRule.Step(DirectionSets.Diagonal);
    private static readonly MovementRule ForwardStep = MovementRule.Step(DirectionSets.Forward);
    private static readonly MovementRule OrthogonalSlide = MovementRule.Slide(DirectionSets.Orthogonal);
    private static readonly MovementRule DiagonalSlide = MovementRule.Slide(DirectionSets.Diagonal);

    public static readonly ImmutableList<GrandPiece> Catalogue =
    [
        // Leaders
        Piece(DragonKing, "Dragon King (Shogi)", Leader, Shogi, OrthogonalSlide, DiagonalStep),
        Piece(FairyGeneral, "Fairy General", Leader, Fairy, Step(DirectionSets.Any), KnightLeap),
        Piece(WesternKing, "King (Western)", Leader, Western, Step(DirectionSets.Any)),
        Piece(XiangqiGeneral, "General (Xiangqi)", Leader, Xiangqi, OrthogonalStep)
            .Restricted(Confined(5)),
        Piece(JanggiGeneral, "General (Janggi)", Leader, Janggi, OrthogonalStep)
            .Restricted(Confined(5)),
        Piece(GoldGeneral, "Gold General (Shogi)", Leader, Shogi, OrthogonalStep, Step(DirectionSets.ForwardDiagonals)),
        Piece(SilverGeneral, "Silver General (Shogi)", Leader, Shogi, DiagonalStep, ForwardStep)
            .Promoting(GoldGeneral),
        Piece(TaflKing, "King (Tafl)", Leader, Tafl, OrthogonalSlide.MoveOnly())
            .Since(CheckersAndTafl)
            .ValuedBefore(MultipleCapturesRepriced, 71),
        Piece(
                CheckersKing,
                "King (Checkers)",
                Leader,
                Checkers,
                DiagonalStep.MoveOnly(),
                MovementRule.Jump(DirectionSets.Diagonal)
            )
            .Restricted(ColourBound(OneColour))
            .Since(CheckersAndTafl)
            .ValuedBefore(MultipleCapturesRepriced, 64),

        // Infantry
        // Unlike the Western Pawn, it takes the piece directly ahead, as it moves (Stated, 2026-11-12): nothing ahead
        // blocks it but its own side's pieces.
        Piece(ShogiPawn, "Pawn (Shogi)", Infantry, Shogi, ForwardStep)
            .Promoting(GoldGeneral),
        Piece(WesternPawn, "Pawn (Western)", Infantry, Western, PawnMovement())
            .Restricted(Moves(BlockedAhead, 2))
            .Promoting(Queen, Rook, Bishop, Knight),
        Piece(JanggiGuard, "Guard (Janggi)", Infantry, Janggi, OrthogonalStep)
            .Restricted(Confined(5)),
        Piece(Noble, "Noble (Makruk)", Infantry, Makruk, DiagonalStep, ForwardStep),
        Piece(JanggiSoldier, "Soldier (Janggi)", Infantry, Janggi, ForwardStep, Sideways())
            .Restricted(
                Moves(NeverBack, 2),
                Area("Diagonally only along palace lines", 2)
            ),
        Piece(Minister, "Minister", Infantry, Fairy, OrthogonalStep),
        Piece(Courtier, "Courtier", Infantry, Fairy, DiagonalStep)
            .Restricted(ColourBound(OneColour)),
        Piece(MakrukPawn, "Pawn (Makruk)", Infantry, Makruk, PawnMovement())
            .Restricted(Moves(BlockedAhead, 2))
            .Promoting(Courtier),
        Piece(XiangqiSoldier, "Soldier (Xiangqi)", Infantry, Xiangqi, ForwardStep, Sideways())
            .Restricted(
                Moves(NeverBack, 2),
                Area(Terrain.River, "Sideways only once across a river", 3)
            ),
        Piece(XiangqiAdvisor, "Advisor (Xiangqi)", Infantry, Xiangqi, DiagonalStep)
            .Restricted(Confined(5)),
        // As in Berolina chess, the Western Pawn turned about: it moves diagonally forward and takes straight ahead,
        // and promotes as the Western Pawn does; without its double first step or en passant (Proposed).
        Piece(
                BerolinaPawn,
                "Pawn (Berolina)",
                Infantry,
                Western,
                Step(DirectionSets.ForwardDiagonals).MoveOnly(),
                ForwardStep.CaptureOnly()
            )
            .Restricted(Moves("Blocked diagonally ahead", 2))
            .Promoting(Queen, Rook, Bishop, Knight)
            .Since(BerolinaManchuAndCourier),
        // Moves exactly as the King does, without a leader's standing (Stated, 2026-11-13).
        Piece(Courier, "Courier", Infantry, Western, Step(DirectionSets.Any))
            .Since(BerolinaManchuAndCourier),

        // Heavy / Slider
        Piece(Lance, "Lance (Shogi)", Heavy, Shogi, MovementRule.Slide(DirectionSets.Forward))
            .Restricted(Moves("Blocked by pieces in its path", 2))
            .Promoting(GoldGeneral),
        Piece(Amazon, "Amazon", Heavy, Fairy, MovementRule.Slide(DirectionSets.Any), KnightLeap),
        Piece(Queen, "Queen (Western)", Heavy, Western, MovementRule.Slide(DirectionSets.Any)),
        Piece(Chancellor, "Chancellor", Heavy, Fairy, OrthogonalSlide, KnightLeap),
        Piece(DragonBishop, "Dragon Bishop", Heavy, Fairy, DiagonalSlide, KnightLeap),
        Piece(DragonHorse, "Dragon Horse (Shogi)", Heavy, Shogi, DiagonalSlide, OrthogonalStep),
        Piece(Rook, "Rook (Western)", Heavy, Western, OrthogonalSlide),
        Piece(Bishop, "Bishop (Western)", Heavy, Western, DiagonalSlide)
            .Restricted(ColourBound(OneColour)),
        Piece(Swede, "Swede", Heavy, Tafl, OrthogonalSlide.MoveOnly())
            .Since(CheckersAndTafl)
            .ValuedBefore(MultipleCapturesRepriced, 11),

        // Cavalry / Leaper
        Piece(Knightrider, "Knightrider", Cavalry, Fairy, MovementRule.Slide(DirectionSets.Knight))
            .Restricted(Moves("Each landing short of the last must be empty", 3)),
        Piece(Zebra, "Zebra", Cavalry, Fairy, Leaper(3, 2)),
        Piece(Knight, "Knight (Western)", Cavalry, Western, KnightLeap),
        Piece(Camel, "Camel", Cavalry, Fairy, Leaper(3, 1))
            .Restricted(ColourBound(OneColour)),
        Piece(Horse, "Horse (Xiangqi)", Cavalry, Xiangqi, KnightLeap.Lame())
            .Restricted(Moves("The first square of its leg must be empty", 2)),
        Piece(JanggiElephant, "Elephant (Janggi)", Cavalry, Janggi, Leaper(3, 2).Lame())
            .Restricted(Moves("Each cell on the way must be empty", 2)),
        Piece(Giraffe, "Giraffe", Cavalry, Fairy, Leaper(4, 1)),
        Piece(Alfil, "Elephant (Alfil)", Cavalry, Fairy, Leaper(2, 2))
            .Restricted(ColourBound("One cell in eight")),
        Piece(Threeleaper, "Threeleaper", Cavalry, Fairy, Leaper(3, 0))
            .Restricted(Bound("One cell in nine")),
        Piece(XiangqiElephant, "Elephant (Xiangqi)", Cavalry, Xiangqi, Leaper(2, 2).Lame())
            .Restricted(
                Moves("The square between must be empty", 2),
                Area(Terrain.River, "May not cross a river, nor stand in one", 3),
                ColourBound("One cell in eight")
            ),
        Piece(WarEngine, "War Engine", Cavalry, Fairy, Leaper(2, 0))
            .Restricted(ColourBound("One cell in four")),

        // Specialty / Artillery
        Piece(
            XiangqiCannon,
            "Cannon (Xiangqi)",
            Artillery,
            Xiangqi,
            OrthogonalSlide.MoveOnly(),
            MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).CaptureOnly()
        ),
        Piece(
                JanggiCannon,
                "Cannon (Janggi)",
                Artillery,
                Janggi,
                MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).Shunning([XiangqiCannon, JanggiCannon])
            )
            .Restricted(Moves("Must hop exactly one piece, and never a Cannon", 3)),
        Piece(Grasshopper, "Grasshopper", Artillery, Fairy, MovementRule.Hop(DirectionSets.Any, 1))
            .Restricted(Moves("Must hop, landing just beyond", 3)),
        Piece(
                Man,
                "Man",
                Artillery,
                Checkers,
                Step(DirectionSets.ForwardDiagonals).MoveOnly(),
                MovementRule.Jump(DirectionSets.ForwardDiagonals)
            )
            .Restricted(ColourBound(OneColour))
            .Promoting(CheckersKing)
            .Since(CheckersAndTafl)
            .ValuedBefore(MultipleCapturesRepriced, 54),
        // Manchu chess's Chariot (its Banner): the Chariot's slide, a Knight's leap (unblocked, Proposed, as the
        // designer's sheet has it), and the Cannon's capture, hopping one piece.
        Piece(
                ManchuChariot,
                "Manchu Chariot",
                Artillery,
                Xiangqi,
                OrthogonalSlide,
                KnightLeap,
                MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).CaptureOnly()
            )
            .Since(BerolinaManchuAndCourier),
    ];

    /// <summary>Every piece of the catalogue, by its key.</summary>
    public static readonly ImmutableDictionary<PieceKindId, GrandPiece> ById =
        Catalogue.ToImmutableDictionary(piece => piece.Id);

    /// <summary>How every piece of the catalogue moves, by its key, for a <c>MoveGenerator</c>.</summary>
    public static readonly ImmutableDictionary<PieceKindId, PieceKind> Kinds =
        Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Kind);

    /// <summary>
    /// The symbol each piece is drawn with, so that it is known at a glance (Stated: the chess symbols for the pieces
    /// named as chess's, the Kings of Checkers and Tafl too, told apart by their abbreviations; the elephant for the
    /// elephants; pieces alike drawn alike, the Pawns sharing one and the
    /// Generals the star, their abbreviations telling them apart; a Dragon as its piece between dragon's wings; the
    /// Knight's kin as the Knight, marked). The Amazon and Chancellor as the Queen and Rook with a Knight's leap, and
    /// the other pieces' symbols, are Proposed.
    /// </summary>
    public static readonly ImmutableDictionary<PieceKindId, string> Symbols =
        ImmutableDictionary<PieceKindId, string>.Empty
            .Add(DragonKing, "dragon-king")
            .Add(FairyGeneral, "star")
            .Add(WesternKing, "king")
            .Add(XiangqiGeneral, "star")
            .Add(JanggiGeneral, "star")
            .Add(GoldGeneral, "star")
            .Add(SilverGeneral, "star")
            .Add(ShogiPawn, "pawn")
            .Add(WesternPawn, "pawn")
            .Add(JanggiGuard, "shield")
            .Add(Noble, "tiara")
            .Add(JanggiSoldier, "sword")
            .Add(Minister, "scroll")
            .Add(Courtier, "plumed-cap")
            .Add(MakrukPawn, "pawn")
            .Add(XiangqiSoldier, "sword")
            .Add(XiangqiAdvisor, "fan")
            .Add(Amazon, "amazon")
            .Add(Queen, "queen")
            .Add(Chancellor, "chancellor")
            .Add(DragonBishop, "dragon-bishop")
            .Add(DragonHorse, "dragon-horse")
            .Add(Lance, "lance")
            .Add(Rook, "rook")
            .Add(Bishop, "bishop")
            .Add(Knightrider, "knightrider")
            .Add(Zebra, "zebra")
            .Add(Knight, "knight")
            .Add(Camel, "camel")
            .Add(Horse, "xiangqi-horse")
            .Add(JanggiElephant, "elephant")
            .Add(Giraffe, "giraffe")
            .Add(Alfil, "elephant")
            .Add(Threeleaper, "dots")
            .Add(XiangqiElephant, "elephant")
            .Add(WarEngine, "catapult")
            .Add(XiangqiCannon, "cannon")
            .Add(JanggiCannon, "cannon")
            .Add(Grasshopper, "grasshopper")
            .Add(CheckersKing, "king")
            .Add(Man, "checker")
            .Add(TaflKing, "king")
            .Add(Swede, "viking-helmet")
            .Add(BerolinaPawn, "pawn")
            .Add(ManchuChariot, "manchu-chariot")
            .Add(Courier, "winged-hat");

    /// <summary>
    /// The abbreviation every piece bears beneath its symbol, beside its class's emblem, each its own (Stated: an
    /// abbreviation on every piece; the Pawns' "Sh", "W" and "M" and the Soldiers' "SJ" and "SX"). The rest are
    /// Proposed: a name's first letter, or its first two, or the first of each word; with the game's after it where
    /// two pieces share a name ("GX" and "GJ", the Generals).
    /// </summary>
    public static readonly ImmutableDictionary<PieceKindId, string> Letters =
        ImmutableDictionary<PieceKindId, string>.Empty
            .Add(DragonKing, "DK")
            .Add(FairyGeneral, "FG")
            .Add(WesternKing, "K")
            .Add(XiangqiGeneral, "GX")
            .Add(JanggiGeneral, "GJ")
            .Add(GoldGeneral, "Go")
            .Add(SilverGeneral, "Si")
            .Add(ShogiPawn, "Sh")
            .Add(WesternPawn, "W")
            .Add(JanggiGuard, "Gu")
            .Add(Noble, "N")
            .Add(JanggiSoldier, "SJ")
            .Add(Minister, "Mi")
            .Add(Courtier, "C")
            .Add(MakrukPawn, "M")
            .Add(XiangqiSoldier, "SX")
            .Add(XiangqiAdvisor, "A")
            .Add(Amazon, "Am")
            .Add(Queen, "Q")
            .Add(Chancellor, "Ch")
            .Add(DragonBishop, "DB")
            .Add(DragonHorse, "DH")
            .Add(Lance, "L")
            .Add(Rook, "R")
            .Add(Bishop, "B")
            .Add(Knightrider, "KR")
            .Add(Zebra, "Z")
            .Add(Knight, "Kn")
            .Add(Camel, "Ca")
            .Add(Horse, "HX")
            .Add(JanggiElephant, "EJ")
            .Add(Giraffe, "Gi")
            .Add(Alfil, "Al")
            .Add(Threeleaper, "Th")
            .Add(XiangqiElephant, "EX")
            .Add(WarEngine, "WE")
            .Add(XiangqiCannon, "CX")
            .Add(JanggiCannon, "CJ")
            .Add(Grasshopper, "G")
            .Add(CheckersKing, "KC")
            .Add(Man, "Ma")
            .Add(TaflKing, "KT")
            .Add(Swede, "Sw")
            .Add(BerolinaPawn, "Be")
            .Add(ManchuChariot, "MC")
            .Add(Courier, "Co");

    /// <summary>
    /// What sort of beast each Cavalry piece is, for people sorting them (Proposed): the Knight's kin, horses; the
    /// elephants; the Camel; any other, other.
    /// </summary>
    public static readonly ImmutableDictionary<PieceKindId, string> Kins =
        ImmutableDictionary<PieceKindId, string>.Empty
            .Add(Knight, "Horse")
            .Add(Horse, "Horse")
            .Add(Knightrider, "Horse")
            .Add(JanggiElephant, "Elephant")
            .Add(Alfil, "Elephant")
            .Add(XiangqiElephant, "Elephant")
            .Add(Camel, "Camel")
            .Add(Zebra, "Other")
            .Add(Giraffe, "Other")
            .Add(Threeleaper, "Other")
            .Add(WarEngine, "Other");

    /// <summary>The emblem each class's pieces bear beneath their symbols (Stated: so the class is told).</summary>
    public static readonly ImmutableDictionary<PieceClass, string> Emblems =
        ImmutableDictionary<PieceClass, string>.Empty
            .Add(Leader, "leader")
            .Add(Infantry, "infantry")
            .Add(Heavy, "heavy")
            .Add(Cavalry, "cavalry")
            .Add(Artillery, "artillery");

    /// <summary>How every piece of the catalogue is marked: its symbol, abbreviation, and class's emblem.</summary>
    public static readonly ImmutableDictionary<PieceKindId, PieceMark> Marks =
        Catalogue.ToImmutableDictionary(
            piece => piece.Id,
            piece => new PieceMark(Symbols[piece.Id], Letters[piece.Id])
            {
                Class = Emblems[piece.Class],
            }
        );

    /// <summary>A piece of the catalogue, worth what the game's table of values says.</summary>
    private static GrandPiece Piece(
        PieceKindId id,
        string name,
        PieceClass pieceClass,
        Tradition tradition,
        params MovementRule[] movement
    ) =>
        new(id, name, pieceClass, tradition, Values[id], [.. movement]);

    /// <summary>A restriction on how the piece moves, lifted for <paramref name="lift"/>.</summary>
    private static Restriction Moves(string description, int lift) => new(RestrictionKind.Movement, description, lift);

    /// <summary>A restriction on where the piece may go, about a palace, lifted for <paramref name="lift"/>.</summary>
    private static Restriction Area(string description, int lift) => Area(Terrain.Palace, description, lift);

    /// <summary>A restriction that keeps the piece within its palace, lifted for <paramref name="lift"/>.</summary>
    private static Restriction Confined(int lift) => Area(Terrain.Palace, Palace, lift) with { IsConfining = true };

    /// <summary>A restriction on where the piece may go, about <paramref name="terrain"/>, lifted at a price.</summary>
    private static Restriction Area(Terrain terrain, string description, int lift) =>
        new(RestrictionKind.Area, description, lift) { Terrain = terrain };

    /// <summary>The share of the board the piece's own move keeps it to: nothing lifts it.</summary>
    private static Restriction Bound(string description) => new(RestrictionKind.Binding, description);

    /// <summary>A share of the board within one colour of square, as <see cref="Bound"/>.</summary>
    private static Restriction ColourBound(string description) =>
        Bound(description) with { IsColourBinding = true };

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
