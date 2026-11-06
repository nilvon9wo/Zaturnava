using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// How a piece is drawn and named, as the player has chosen (<see cref="PieceSet"/>). Chaturaji: as a
/// silhouette of what it is (a crown for the Raja, an elephant, a horse's head, a boat), by its own
/// name. Chess: as the chess piece that moves most like it (the Elephant moves like a Rook), by that
/// piece's name - but the Boat, which chess lacks, as a boat. Either way a Pawn is a chess pawn, and
/// any other kind is its name's first letter. Kinds are known only by their ids, so a ruleset with
/// other pieces still draws.
/// </summary>
public static partial class PieceStyle
{
    /// <summary>Asks for the text (not the emoji) form of the chess symbols, so that they take a colour.</summary>
    private const string TextPresentation = "\uFE0E";

    private const string Boat =
        "M-0.27 0.1H0.27L0.17 0.24H-0.17Z"
            + "M-0.02 0.08V-0.3H0.02V0.08Z"
            + "M0.05 -0.27L0.23 0.05H0.05Z"
            + "M-0.05 -0.18V0.05H-0.21Z";

    private static readonly ImmutableDictionary<string, string> ChessSymbols =
        ImmutableDictionary<string, string>.Empty
            .Add("raja", "\u265A")
            .Add("elephant", "\u265C")
            .Add("horse", "\u265E")
            .Add("pawn", "\u265F");

    private const string Crown =
        "M-0.25 0.18H0.25L0.28 -0.14L0.13 0L0 -0.22L-0.13 0L-0.28 -0.14Z"
            + "M-0.25 0.17V0.26H0.25V0.17Z";

    private const string HorseHead =
        "M0.18 0.26H-0.12L-0.06 0.06L-0.24 0.02L-0.27 -0.08L-0.06 -0.24L0 -0.32L0.05 -0.22"
            + "Q0.24 -0.12 0.18 0.26Z";

    private const string Elephant =
        "M-0.14 0A0.2 0.14 0 1 0 0.26 0A0.2 0.14 0 1 0 -0.14 0Z"
            + "M-0.27 -0.06A0.11 0.11 0 1 0 -0.05 -0.06A0.11 0.11 0 1 0 -0.27 -0.06Z"
            + "M-0.27 -0.04Q-0.33 0.12 -0.26 0.24L-0.21 0.22Q-0.26 0.12 -0.2 0Z"
            + "M-0.08 0.08V0.26H-0.01V0.08Z"
            + "M0.03 0.08V0.26H0.1V0.08Z"
            + "M0.14 0.08V0.26H0.21V0.08Z";

    /// <summary>Two dragon's wings, either side of the piece a Dragon is drawn as.</summary>
    private const string Wings =
        "M0.24 -0.05L0.3 -0.27Q0.39 -0.21 0.44 -0.1L0.39 -0.08L0.41 0.02L0.33 0L0.3 0.1Z"
            + "M-0.24 -0.05L-0.3 -0.27Q-0.39 -0.21 -0.44 -0.1L-0.39 -0.08L-0.41 0.02L-0.33 0L-0.3 0.1Z";

    /// <summary>A small horseshoe, at the right of a piece that also leaps as a Knight does.</summary>
    private const string KnightsLeap =
        "M0.27 0.06V0.14A0.09 0.09 0 0 0 0.45 0.14V0.06H0.41V0.14A0.05 0.05 0 0 1 0.31 0.14V0.06Z";

    /// <summary>
    /// SVG path data for each symbol a marked piece is drawn with (Proposed), about the piece's centre as a shape is,
    /// alike for pieces alike (Stated): a star for every General; a shield for the Guard, a fan for the Advisor, a
    /// scroll for the Minister, a tiara for the Noble, a key for the Courtier, a sword for a Soldier; a lance with its
    /// pennant; a camel, an elephant, a giraffe, three dots for the Threeleaper, a catapult for the War Engine; a
    /// cannon on its wheel, and a grasshopper. A piece drawn as a chess piece with more to it has here what it adds
    /// (<see cref="SymbolGlyphs"/>): a Dragon its wings; the Amazon and Chancellor a small horseshoe, as
    /// they leap as a Knight does too; and the Knight's kin the mark of how they differ: the Knightrider speed lines,
    /// as it rides on, the Zebra stripes, and the Xiangqi Horse a block, as its leap may be blocked.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> Symbols =
        ImmutableDictionary<string, string>.Empty
            .Add(
                "star",
                "M0 -0.3L0.071 -0.097L0.285 -0.093L0.114 0.037L0.176 0.243L0 0.12L-0.176 0.243L-0.114 0.037"
                    + "L-0.285 -0.093L-0.071 -0.097Z"
            )
            .Add("dragon-king", Wings)
            .Add("dragon-bishop", Wings)
            .Add("dragon-horse", Wings)
            .Add("amazon", KnightsLeap)
            .Add("chancellor", KnightsLeap)
            .Add(
                "knightrider",
                "M-0.46 -0.14H-0.26V-0.1H-0.46Z" + "M-0.42 -0.02H-0.26V0.02H-0.42Z" + "M-0.46 0.1H-0.26V0.14H-0.46Z"
            )
            .Add(
                "zebra",
                "M0.26 -0.2H0.31L0.37 0.1H0.32Z"
                    + "M0.36 -0.2H0.41L0.45 0.02H0.4Z"
                    + "M-0.31 -0.2H-0.26L-0.32 0.1H-0.37Z"
                    + "M-0.41 -0.2H-0.36L-0.4 0.02H-0.45Z"
            )
            .Add("xiangqi-horse", "M-0.44 -0.06H-0.28V0.1H-0.44Z")
            .Add("shield", "M-0.24 -0.28H0.24V0Q0.24 0.18 0 0.3Q-0.24 0.18 -0.24 0Z")
            .Add("fan", "M0 0.2L-0.28 -0.08A0.3 0.3 0 0 1 0.28 -0.08Z" + "M-0.03 0.18H0.03V0.3H-0.03Z")
            .Add(
                "scroll",
                "M-0.18 -0.2H0.18V0.2H-0.18Z"
                    + "M-0.26 -0.26H0.26V-0.16H-0.26Z"
                    + "M-0.26 0.16H0.26V0.26H-0.26Z"
                    + Circle(-0.26, -0.21, 0.05)
                    + Circle(0.26, -0.21, 0.05)
                    + Circle(-0.26, 0.21, 0.05)
                    + Circle(0.26, 0.21, 0.05)
            )
            .Add(
                "tiara",
                "M-0.26 0.12L-0.22 -0.1L-0.1 0.02L0 -0.26L0.1 0.02L0.22 -0.1L0.26 0.12Z"
                    + "M-0.26 0.14H0.26V0.24H-0.26Z"
            )
            .Add(
                "key",
                Circle(0, -0.18, 0.12)
                    + "M-0.05 -0.18A0.05 0.05 0 1 0 0.05 -0.18A0.05 0.05 0 1 0 -0.05 -0.18Z"
                    + "M-0.03 -0.07H0.03V0.28H-0.03Z"
                    + "M0.03 0.12H0.12V0.17H0.03Z"
                    + "M0.03 0.22H0.1V0.27H0.03Z"
            )
            .Add(
                "sword",
                "M0 -0.3L0.045 -0.22V0.1H-0.045V-0.22Z"
                    + "M-0.16 0.1H0.16V0.15H-0.16Z"
                    + "M-0.03 0.15H0.03V0.25H-0.03Z"
                    + Circle(0, 0.27, 0.04)
            )
            .Add(
                "lance",
                "M0 -0.3L0.06 -0.16H-0.06Z" + "M-0.025 -0.16H0.025V0.3H-0.025Z" + "M0.025 -0.14L0.22 -0.08L0.025 -0.02Z"
            )
            .Add(
                "camel",
                "M-0.3 -0.14L-0.22 -0.2L-0.18 -0.12L-0.16 0Q-0.1 -0.24 -0.02 -0.04Q0.06 -0.24 0.14 -0.04L0.24 0"
                    + "L0.24 0.1L0.2 0.1L0.2 0.28L0.15 0.28L0.15 0.1L-0.05 0.1L-0.05 0.28L-0.1 0.28L-0.1 0.1"
                    + "L-0.16 0.08L-0.24 -0.08L-0.3 -0.08Z"
            )
            .Add("elephant", Elephant)
            .Add(
                "giraffe",
                "M-0.06 -0.3L0.06 -0.3L0.08 -0.24L0.02 -0.22L0.02 0.02L0.2 0.06L0.2 0.28L0.15 0.28L0.15 0.12"
                    + "L-0.1 0.12L-0.1 0.28L-0.15 0.28L-0.15 0.06L-0.04 0L-0.04 -0.22Z"
            )
            .Add("dots", Circle(0, -0.2, 0.07) + Circle(0, 0, 0.07) + Circle(0, 0.2, 0.07))
            .Add(
                "catapult",
                "M-0.26 0.14H0.26V0.22H-0.26Z"
                    + Circle(-0.17, 0.24, 0.06)
                    + Circle(0.17, 0.24, 0.06)
                    + "M-0.2 0.12L0.18 -0.24L0.23 -0.19L-0.15 0.16Z"
                    + "M-0.04 0.14L0.04 -0.04L0.12 0.14Z"
                    + Circle(0.2, -0.22, 0.07)
            )
            .Add(
                "cannon",
                "M-0.28 0.02L0.2 -0.22L0.26 -0.1L-0.2 0.14Z"
                    + "M-0.2 0.14A0.12 0.12 0 1 1 0.04 0.14A0.12 0.12 0 1 1 -0.2 0.14Z"
            )
            .Add(
                "grasshopper",
                "M-0.26 0A0.2 0.07 0 1 1 0.14 0A0.2 0.07 0 1 1 -0.26 0Z"
                    + Circle(0.2, -0.02, 0.06)
                    + "M-0.02 -0.02L-0.16 -0.22L-0.11 -0.24L0.03 -0.04Z"
                    + "M-0.16 -0.22L-0.11 -0.2L-0.25 0.21L-0.3 0.2Z"
                    + "M0.08 0.04L0.12 0.2H0.08L0.04 0.05Z"
                    + "M0.22 -0.07L0.28 -0.26L0.3 -0.25L0.25 -0.06Z"
            );

    /// <summary>A frame, as a military map marks a unit, hollow, for a class's mark to stand in.</summary>
    private const string Frame = "M-0.3 -0.2H0.3V0.2H-0.3Z" + "M-0.23 -0.13V0.13H0.23V-0.13Z";

    /// <summary>A stroke across a frame, from its lower left to its upper right, as a map marks cavalry.</summary>
    private const string Slash = "M0.23 -0.13V-0.1L-0.17 0.13H-0.23V0.1L0.17 -0.13Z";

    /// <summary>
    /// SVG path data for each class's emblem (Proposed), drawn small beneath a piece's symbol, each unlike any
    /// piece's symbol (Stated): the frames a military map marks units with. A leader's is solid, on a staff, as a
    /// headquarters'; infantry's crossed; a heavy piece's holds an oval, as armour's does; cavalry's has one stroke
    /// across; artillery's a dot.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> ClassEmblems =
        ImmutableDictionary<string, string>.Empty
            .Add("leader", "M-0.3 -0.2H0.3V0.2H-0.3Z" + "M-0.3 0.2H-0.24V0.34H-0.3Z")
            .Add("infantry", Frame + "M-0.23 -0.13H-0.17L0.23 0.1V0.13H0.17L-0.23 -0.1Z" + Slash)
            .Add(
                "heavy",
                Frame
                    + "M-0.17 0A0.17 0.09 0 1 1 0.17 0A0.17 0.09 0 1 1 -0.17 0Z"
                    + "M-0.11 0A0.11 0.04 0 1 0 0.11 0A0.11 0.04 0 1 0 -0.11 0Z"
            )
            .Add("cavalry", Frame + Slash)
            .Add("artillery", Frame + Circle(0, 0, 0.07));

    /// <summary>
    /// The symbols drawn as chess's pieces, as its glyphs: King, Queen, Rook, Bishop, Knight, Pawn; and the pieces
    /// that are one of those with more to it (Stated: the Dragons, the Knight's kin), each with what it adds drawn
    /// round it (<see cref="Symbols"/>). The Amazon is a Queen that leaps too, the Chancellor a Rook (Proposed).
    /// </summary>
    private static readonly ImmutableDictionary<string, string> SymbolGlyphs =
        ImmutableDictionary<string, string>.Empty
            .Add("king", "♚")
            .Add("queen", "♛")
            .Add("rook", "♜")
            .Add("bishop", "♝")
            .Add("knight", "♞")
            .Add("pawn", "♟")
            .Add("dragon-king", "♚")
            .Add("dragon-bishop", "♝")
            .Add("dragon-horse", "♞")
            .Add("amazon", "♛")
            .Add("chancellor", "♜")
            .Add("knightrider", "♞")
            .Add("zebra", "♞")
            .Add("xiangqi-horse", "♞");

    private static readonly ImmutableDictionary<string, string> ChaturajiSymbols =
        ImmutableDictionary<string, string>.Empty.Add("pawn", "\u265F");

    /// <summary>
    /// SVG path data for the kinds drawn as shapes, about a piece's centre, in board units, y down, to
    /// fill like a glyph, each part drawn the same way round so that overlaps stay filled: a crown on
    /// its band; an elephant, trunk down, on four legs; a horse's head; a sailing boat.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> ChaturajiShapes =
        ImmutableDictionary<string, string>.Empty
            .Add("raja", Crown)
            .Add("elephant", Elephant)
            .Add("horse", HorseHead)
            .Add("boat", Boat);

    private static readonly ImmutableDictionary<string, string> ChessShapes =
        ImmutableDictionary<string, string>.Empty.Add("boat", Boat);

    private static readonly ImmutableDictionary<string, string> ChessNames =
        ImmutableDictionary<string, string>.Empty
            .Add("Raja", "King")
            .Add("Elephant", "Rook")
            .Add("Horse", "Knight");

    public static string GlyphOf([Required] PieceDto piece, PieceSet set) => GlyphOf(piece.Kind, piece.Name, set);

    public static string GlyphOf([Required] string kind, [Required] string name, PieceSet set) =>
        (set == PieceSet.Chess ? ChessSymbols : ChaturajiSymbols).TryGetValue(kind, out string? symbol)
            ? symbol + TextPresentation
            : name[..1];

    /// <summary>
    /// Whether the set chosen changes any of <paramref name="kinds"/>: the Raja, Elephant and Horse are drawn and
    /// named as chess pieces; every other kind, as a game with other pieces has (Grand Legions), stays as it is, so
    /// such a game offers no choice of set, and shows its pieces by its own names.
    /// </summary>
    public static bool HasChessForms([Required] IEnumerable<string> kinds) =>
        kinds.Any(kind => ChessSymbols.ContainsKey(kind) && ChaturajiShapes.ContainsKey(kind));

    /// <summary>How far below its piece's centre a marked piece's class and letters are, in board units.</summary>
    public const double LettersDrop = 0.24;

    /// <summary>How far right of its piece's centre a marked piece's letters are, beside its class emblem.</summary>
    public const double LettersShift = 0.09;

    /// <summary>How a symbol is placed about its piece's centre: a little shrunk, and raised, over its class.</summary>
    public const string EmblemPlacement = "translate(0 -0.07) scale(0.66)";

    /// <summary>How far above its piece's centre a symbol drawn as a glyph is written, in board units.</summary>
    public const double GlyphRise = 0.07;

    /// <summary>
    /// A marked piece's symbol as SVG path data: the whole of it, or what it adds round its glyph; null for one drawn
    /// as a glyph alone, or not known here.
    /// </summary>
    public static string? EmblemOf([Required] string emblem) => Symbols.GetValueOrDefault(emblem);

    /// <summary>
    /// A marked piece's symbol, as a chess glyph; null for one drawn as a shape alone, or not known here.
    /// </summary>
    public static string? EmblemGlyphOf([Required] string emblem) =>
        SymbolGlyphs.TryGetValue(emblem, out string? glyph) ? glyph + TextPresentation : null;

    /// <summary>A class's emblem, as SVG path data; null for a class not known here.</summary>
    public static string? ClassEmblemOf([Required] string pieceClass) => ClassEmblems.GetValueOrDefault(pieceClass);

    /// <summary>
    /// How a class's emblem is placed, small, beneath its piece's symbol: in the middle, alone; at the left, where
    /// the piece's letters follow it.
    /// </summary>
    public static string ClassPlacementBeside([Required(AllowEmptyStrings = true)] string letters) =>
        $"translate({(letters.Length == 0 ? "0" : "-0.1")} {Number(LettersDrop)}) scale(0.28)";

    /// <summary>A circle, as SVG path data, drawn the same way round as every shape here.</summary>
    private static string Circle(double x, double y, double radius)
    {
        string left = Number(x - radius);
        string right = Number(x + radius);
        string across = $"{Number(radius)} {Number(radius)} 0 1 1";
        return $"M{left} {Number(y)}A{across} {right} {Number(y)}A{across} {left} {Number(y)}Z";
    }

    private static string Number(double value) =>
        Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);

    /// <summary>The kind's shape, as SVG path data; null for a kind drawn as a glyph.</summary>
    public static string? ShapeOf([Required] string kind, PieceSet set) =>
        (set == PieceSet.Chess ? ChessShapes : ChaturajiShapes).GetValueOrDefault(kind);

    /// <summary>
    /// The text as the set names the pieces: "Black took Red's Rook (+4)." for the chess set, where the
    /// server wrote "Elephant"; unchanged for the chaturaji set.
    /// </summary>
    public static string Named([Required] string text, PieceSet set) =>
        set == PieceSet.Chess
            ? ChaturajiNames().Replace(text, match => ChessNames[match.Groups[1].Value] + match.Groups[2].Value)
            : text;

    [GeneratedRegex(@"\b(Raja|Elephant|Horse)(s?)\b", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ChaturajiNames();
}
