using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// How a piece is drawn and named, as the player has chosen (<see cref="PieceSet"/>). Traditional: as a
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

    private static readonly ImmutableDictionary<string, string> TraditionalSymbols =
        ImmutableDictionary<string, string>.Empty.Add("pawn", "\u265F");

    /// <summary>
    /// SVG path data for the kinds drawn as shapes, about a piece's centre, in board units, y down, to
    /// fill like a glyph, each part drawn the same way round so that overlaps stay filled: a crown on
    /// its band; an elephant, trunk down, on four legs; a horse's head; a sailing boat.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> TraditionalShapes =
        ImmutableDictionary<string, string>.Empty
            .Add(
                "raja",
                "M-0.25 0.18H0.25L0.28 -0.14L0.13 0L0 -0.22L-0.13 0L-0.28 -0.14Z"
                    + "M-0.25 0.17V0.26H0.25V0.17Z"
            )
            .Add(
                "elephant",
                "M-0.14 0A0.2 0.14 0 1 0 0.26 0A0.2 0.14 0 1 0 -0.14 0Z"
                    + "M-0.27 -0.06A0.11 0.11 0 1 0 -0.05 -0.06A0.11 0.11 0 1 0 -0.27 -0.06Z"
                    + "M-0.27 -0.04Q-0.33 0.12 -0.26 0.24L-0.21 0.22Q-0.26 0.12 -0.2 0Z"
                    + "M-0.08 0.08V0.26H-0.01V0.08Z"
                    + "M0.03 0.08V0.26H0.1V0.08Z"
                    + "M0.14 0.08V0.26H0.21V0.08Z"
            )
            .Add(
                "horse",
                "M0.18 0.26H-0.12L-0.06 0.06L-0.24 0.02L-0.27 -0.08L-0.06 -0.24L0 -0.32L0.05 -0.22"
                    + "Q0.24 -0.12 0.18 0.26Z"
            )
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
        (set == PieceSet.Chess ? ChessSymbols : TraditionalSymbols).TryGetValue(kind, out string? symbol)
            ? symbol + TextPresentation
            : name[..1];

    /// <summary>The kind's shape, as SVG path data; null for a kind drawn as a glyph.</summary>
    public static string? ShapeOf([Required] string kind, PieceSet set) =>
        (set == PieceSet.Chess ? ChessShapes : TraditionalShapes).GetValueOrDefault(kind);

    /// <summary>
    /// The text as the set names the pieces: "Black took Red's Rook (+4)." for the chess set, where the
    /// server wrote "Elephant"; unchanged for the traditional set.
    /// </summary>
    public static string Named([Required] string text, PieceSet set) =>
        set == PieceSet.Chess
            ? TraditionalNames().Replace(text, match => ChessNames[match.Groups[1].Value] + match.Groups[2].Value)
            : text;

    [GeneratedRegex(@"\b(Raja|Elephant|Horse)(s?)\b", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TraditionalNames();
}
