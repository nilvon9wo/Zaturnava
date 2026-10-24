using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// How a piece is drawn: as a shape of its own where it has one (the boat, which chess lacks); else as
/// the chess piece that moves most like it, where one does (the elephant moves like a rook); and
/// otherwise as its name's first letter. Kinds are known only by their ids, so a ruleset with other
/// pieces still draws.
/// </summary>
public static class PieceStyle
{
    /// <summary>Asks for the text (not the emoji) form of the chess symbols, so that they take a colour.</summary>
    private const string TextPresentation = "︎";

    private static readonly ImmutableDictionary<string, string> ChessSymbols =
        ImmutableDictionary<string, string>.Empty
            .Add("raja", "♚")
            .Add("elephant", "♜")
            .Add("horse", "♞")
            .Add("pawn", "♟");

    /// <summary>
    /// SVG path data for the kinds drawn as shapes, about a piece's centre, in board units, y down,
    /// to fill like a glyph: a sailing boat, hull, mast and two sails.
    /// </summary>
    private static readonly ImmutableDictionary<string, string> Shapes =
        ImmutableDictionary<string, string>.Empty
            .Add(
                "boat",
                "M-0.27 0.1H0.27L0.17 0.24H-0.17Z"
                    + "M-0.02 0.08V-0.3H0.02V0.08Z"
                    + "M0.05 -0.27L0.23 0.05H0.05Z"
                    + "M-0.05 -0.18V0.05H-0.21Z"
            );

    public static string GlyphOf([Required] PieceDto piece) => GlyphOf(piece.Kind, piece.Name);

    public static string GlyphOf([Required] string kind, [Required] string name) =>
        ChessSymbols.TryGetValue(kind, out string? symbol)
            ? symbol + TextPresentation
            : name[..1];

    /// <summary>The kind's shape, as SVG path data; null for a kind drawn as a glyph.</summary>
    public static string? ShapeOf([Required] string kind) => Shapes.GetValueOrDefault(kind);
}
