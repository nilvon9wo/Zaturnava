using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// How a piece is drawn: as the chess piece that moves most like it, where one does (the elephant moves
/// like a rook, the boat leaps diagonally like a bishop's shadow), and otherwise as its name's first
/// letter. Kinds are known only by their ids, so a ruleset with other pieces still draws.
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
            .Add("boat", "♝")
            .Add("pawn", "♟");

    public static string GlyphOf([Required] PieceDto piece) =>
        ChessSymbols.TryGetValue(piece.Kind, out string? symbol)
            ? symbol + TextPresentation
            : piece.Name[..1];
}
