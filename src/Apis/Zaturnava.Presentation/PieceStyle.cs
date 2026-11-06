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

    /// <summary>
    /// SVG path data for each symbol a marked piece is drawn with (Proposed), each its own, about the piece's centre
    /// as a shape is: drawn whole where it alone tells the piece, small above the letters where pieces sharing a name
    /// share it. A star for the Fairy General, a flag for a General, the sun for the Gold General and the moon for
    /// the Silver; a shield for the Guard, a fan for the Advisor, a scroll for the Minister, a tiara for the Noble, a
    /// key for the Courtier, a sword for a Soldier; a lance with its pennant, a bow and arrow for the Amazon, scales
    /// for the Chancellor, a cross for the Archbishop, a dragon's wings for a Dragon; two chevrons for the
    /// Knightrider, stripes for the Zebra, a camel, a horseshoe for the Xiangqi Horse, an elephant, a giraffe, three
    /// dots for the Threeleaper, a catapult for the War Engine; a cannon on its wheel, and a grasshopper.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> Symbols =
        ImmutableDictionary<string, string>.Empty
            .Add(
                "star",
                "M0 -0.3L0.071 -0.097L0.285 -0.093L0.114 0.037L0.176 0.243L0 0.12L-0.176 0.243L-0.114 0.037"
                    + "L-0.285 -0.093L-0.071 -0.097Z"
            )
            .Add(
                "general",
                "M-0.2 -0.3H-0.14V0.3H-0.2Z"
                    + "M-0.14 -0.3H0.26L0.16 -0.18L0.26 -0.06H-0.14Z"
                    + "M-0.27 0.24H-0.07V0.3H-0.27Z"
            )
            .Add(
                "sun",
                Circle(0, 0, 0.15)
                    + "M-0.05 -0.17L0 -0.3L0.05 -0.17Z"
                    + "M0.17 -0.05L0.3 0L0.17 0.05Z"
                    + "M0.05 0.17L0 0.3L-0.05 0.17Z"
                    + "M-0.17 0.05L-0.3 0L-0.17 -0.05Z"
                    + "M0.085 -0.155L0.212 -0.212L0.155 -0.085Z"
                    + "M0.155 0.085L0.212 0.212L0.085 0.155Z"
                    + "M-0.085 0.155L-0.212 0.212L-0.155 0.085Z"
                    + "M-0.155 -0.085L-0.212 -0.212L-0.085 -0.155Z"
            )
            .Add("moon", "M0.06 -0.27A0.28 0.28 0 1 0 0.06 0.27A0.3 0.3 0 0 1 0.06 -0.27Z")
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
                "bow",
                "M-0.22 -0.28A0.3 0.3 0 0 1 -0.22 0.28V0.2A0.24 0.24 0 0 0 -0.22 -0.2Z"
                    + "M-0.23 -0.28H-0.21V0.28H-0.23Z"
                    + "M-0.26 -0.02H0.2V0.02H-0.26Z"
                    + "M0.2 -0.07L0.3 0L0.2 0.07Z"
            )
            .Add(
                "scales",
                "M-0.025 -0.22H0.025V0.24H-0.025Z"
                    + "M-0.14 0.24H0.14V0.29H-0.14Z"
                    + "M-0.26 -0.22H0.26V-0.17H-0.26Z"
                    + "M-0.225 -0.17H-0.215V0.02H-0.225Z"
                    + "M0.215 -0.17H0.225V0.02H0.215Z"
                    + "M-0.3 0.02A0.08 0.08 0 0 0 -0.14 0.02Z"
                    + "M0.14 0.02A0.08 0.08 0 0 0 0.3 0.02Z"
            )
            .Add(
                "cross",
                "M-0.05 -0.3H0.05V-0.12H0.2V-0.02H0.05V0.3H-0.05V-0.02H-0.2V-0.12H-0.05Z"
            )
            .Add(
                "dragon",
                "M-0.3 -0.1Q-0.15 -0.3 0 -0.12Q0.15 -0.3 0.3 -0.1L0.2 0L0.12 -0.04L0.06 0.06L0 0L-0.06 0.06"
                    + "L-0.12 -0.04L-0.2 0Z"
                    + "M-0.04 -0.12H0.04V0.22L0 0.3L-0.04 0.22Z"
            )
            .Add(
                "chevrons",
                "M-0.26 -0.02L0 -0.28L0.26 -0.02V0.08L0 -0.18L-0.26 0.08Z"
                    + "M-0.26 0.18L0 -0.08L0.26 0.18V0.28L0 0.02L-0.26 0.28Z"
            )
            .Add(
                "stripes",
                "M-0.3 -0.26H-0.18L-0.02 0.26H-0.14Z"
                    + "M-0.08 -0.26H0.04L0.2 0.26H0.08Z"
                    + "M0.14 -0.26H0.26L0.3 -0.13L0.3 0.26Z"
            )
            .Add(
                "camel",
                "M-0.3 -0.14L-0.22 -0.2L-0.18 -0.12L-0.16 0Q-0.1 -0.24 -0.02 -0.04Q0.06 -0.24 0.14 -0.04L0.24 0"
                    + "L0.24 0.1L0.2 0.1L0.2 0.28L0.15 0.28L0.15 0.1L-0.05 0.1L-0.05 0.28L-0.1 0.28L-0.1 0.1"
                    + "L-0.16 0.08L-0.24 -0.08L-0.3 -0.08Z"
            )
            .Add(
                "horseshoe",
                "M-0.24 -0.26V0A0.24 0.24 0 0 0 0.24 0V-0.26H0.14V0A0.14 0.14 0 0 1 -0.14 0V-0.26Z"
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

    /// <summary>
    /// SVG path data for each class's emblem (Proposed), drawn small beneath a piece's symbol: a crown for a leader,
    /// a shield for infantry, a battlemented tower for a heavy piece, a horse's head for cavalry, and a cannon on its
    /// wheel for artillery.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> ClassEmblems =
        ImmutableDictionary<string, string>.Empty
            .Add("leader", Crown)
            .Add("infantry", "M-0.24 -0.28H0.24V0Q0.24 0.18 0 0.3Q-0.24 0.18 -0.24 0Z")
            .Add(
                "heavy",
                "M-0.2 0.26V-0.1H-0.26V-0.3H-0.15V-0.2H-0.05V-0.3H0.05V-0.2H0.15V-0.3H0.26V-0.1H0.2V0.26Z"
            )
            .Add("cavalry", HorseHead)
            .Add(
                "artillery",
                "M-0.28 0.02L0.2 -0.22L0.26 -0.1L-0.2 0.14Z"
                    + "M-0.2 0.14A0.12 0.12 0 1 1 0.04 0.14A0.12 0.12 0 1 1 -0.2 0.14Z"
            );

    /// <summary>The symbols of the pieces chess has, as its glyphs: King, Queen, Rook, Bishop, Knight, Pawn.</summary>
    private static readonly ImmutableDictionary<string, string> SymbolGlyphs =
        ImmutableDictionary<string, string>.Empty
            .Add("king", "♚")
            .Add("queen", "♛")
            .Add("rook", "♜")
            .Add("bishop", "♝")
            .Add("knight", "♞")
            .Add("pawn", "♟");

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
    public const double LettersShift = 0.07;

    /// <summary>How a symbol is placed about its piece's centre: a little shrunk, and raised, over its class.</summary>
    public const string EmblemPlacement = "translate(0 -0.07) scale(0.66)";

    /// <summary>How far above its piece's centre a symbol drawn as a glyph is written, in board units.</summary>
    public const double GlyphRise = 0.07;

    /// <summary>A marked piece's symbol, as SVG path data; null for one drawn as a glyph, or not known here.</summary>
    public static string? EmblemOf([Required] string emblem) => Symbols.GetValueOrDefault(emblem);

    /// <summary>A marked piece's symbol, as a chess glyph; null for one drawn as a shape, or not known here.</summary>
    public static string? EmblemGlyphOf([Required] string emblem) =>
        SymbolGlyphs.TryGetValue(emblem, out string? glyph) ? glyph + TextPresentation : null;

    /// <summary>A class's emblem, as SVG path data; null for a class not known here.</summary>
    public static string? ClassEmblemOf([Required] string pieceClass) => ClassEmblems.GetValueOrDefault(pieceClass);

    /// <summary>
    /// How a class's emblem is placed, small, beneath its piece's symbol: in the middle, alone; at the left, where
    /// the piece's letters follow it.
    /// </summary>
    public static string ClassPlacementBeside([Required(AllowEmptyStrings = true)] string letters) =>
        $"translate({(letters.Length == 0 ? "0" : "-0.09")} {Number(LettersDrop)}) scale(0.3)";

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
