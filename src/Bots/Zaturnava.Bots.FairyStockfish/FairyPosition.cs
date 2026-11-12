using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>
/// A game, as Fairy-Stockfish can see it, from one seat's side (Proposed): two sides, not four seats, the seat's team
/// White, to move, and every other team Black; each piece a piece of its own kind for each way it moves, facing as
/// its seat faces (<see cref="BetzaNotation"/>), so that a Pawn facing east is not one facing north. There is no
/// king: a side that loses every leader (<see cref="IRulesApplying.IsLeader"/>) has lost. What the variant is called
/// (<paramref name="Name"/>), how it is defined (<paramref name="Definition"/>, a section of a <c>variants.ini</c>),
/// and where the pieces stand (<paramref name="Fen"/>).
/// </summary>
public sealed record FairyPosition(string Name, string Definition, string Fen)
{
    /// <summary>The most files Fairy-Stockfish's large-board build plays on.</summary>
    public const int MostFiles = 12;

    /// <summary>The most ranks Fairy-Stockfish's large-board build plays on.</summary>
    public const int MostRanks = 10;

    /// <summary>The most kinds of piece a variant may define (customPiece1 to customPiece25).</summary>
    public const int MostKinds = 25;

    private const string Letters = "abcdefghijklmnopqrstuvwxy";

    /// <summary>
    /// The game from <paramref name="seat"/>'s side; none, if Fairy-Stockfish cannot see it so: a board not a
    /// rectangle of squares it can hold, a piece it cannot define, or too many kinds of piece.
    /// </summary>
    public static FairyPosition? Of([Required] IRulesApplying rules, [Required] GameState state, SeatId seat)
    {
        ICellNavigating board = rules.Topology;
        ImmutableList<CellLayout> cells = [.. board.Cells.Select(board.LayoutOf)];
        int files = (int)cells.Max(cell => cell.X) + 1;
        int ranks = (int)cells.Max(cell => cell.Y) + 1;
        if (files > MostFiles || ranks > MostRanks || cells.Count != files * ranks)
        {
            return null;
        }

        ImmutableDictionary<SeatId, Direction> facing =
            state.Seats.ToImmutableDictionary(at => at.Id, at => at.Forward);
        ImmutableList<(CellLayout Cell, bool IsWhite, string? Moves, bool IsLeader)> pieces =
        [
            .. state.Position.Occupants.Select(occupant =>
            {
                bool isWhite = state.Teams.AreAllied(occupant.Value.Owner, seat);
                return (
                    board.LayoutOf(occupant.Key),
                    isWhite,
                    BetzaNotation.Of(
                        rules.GuideOf(occupant.Value.Kind).Kind,
                        board,
                        facing[occupant.Value.Owner],
                        !isWhite
                    ),
                    rules.IsLeader(occupant.Value.Kind)
                );
            }),
        ];
        ImmutableList<string> kinds =
        [
            .. pieces
                .Select(piece => piece.Moves)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
        if (pieces.Any(piece => piece.Moves is null) || kinds.Count > MostKinds)
        {
            return null;
        }

        ImmutableDictionary<string, char> letters = kinds
            .Select((moves, at) => (Moves: moves, Letter: Letters[at]))
            .ToImmutableDictionary(kind => kind.Moves, kind => kind.Letter);
        ImmutableHashSet<char> leaders =
            [.. pieces.Where(piece => piece.IsLeader).Select(piece => letters[piece.Moves!])];
        string fen = FenOf(files, ranks, pieces.ToImmutableDictionary(
            piece => ((int)piece.Cell.X, (int)piece.Cell.Y),
            piece => piece.IsWhite ? char.ToUpperInvariant(letters[piece.Moves!]) : letters[piece.Moves!]
        ));
        string body = DefinitionOf(files, ranks, kinds, letters, leaders);
        string name = "zaturnava-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)))[..16];
        return new FairyPosition(name, $"[{name}]\n{body}", fen);
    }

    /// <summary>A square's name, as Fairy-Stockfish writes it in a move: its file's letter, then its rank.</summary>
    public static string SquareOf(CellLayout cell) =>
        string.Create(CultureInfo.InvariantCulture, $"{(char)('a' + (int)cell.X)}{(int)cell.Y + 1}");

    private static string DefinitionOf(
        int files,
        int ranks,
        ImmutableList<string> kinds,
        ImmutableDictionary<string, char> letters,
        ImmutableHashSet<char> leaders
    )
    {
        StringBuilder text = new();
        _ = text.Append(CultureInfo.InvariantCulture, $"maxFile = {files}\n")
            .Append(CultureInfo.InvariantCulture, $"maxRank = {ranks}\n")
            .Append("pawn = -\nknight = -\nbishop = -\nrook = -\nqueen = -\nking = -\n")
            .Append("castling = false\ndoubleStep = false\n")
            .Append("stalemateValue = draw\nextinctionValue = loss\n")
            .Append(CultureInfo.InvariantCulture, $"extinctionPieceTypes = {string.Concat(leaders.Order())}\n");
        foreach ((string moves, int at) in kinds.Select((moves, at) => (moves, at)))
        {
            _ = text.Append(CultureInfo.InvariantCulture, $"customPiece{at + 1} = {letters[moves]}:{moves}\n");
        }

        return text.ToString();
    }

    /// <summary>Where the pieces stand, rank by rank from the top, and White to move.</summary>
    private static string FenOf(int files, int ranks, ImmutableDictionary<(int File, int Rank), char> pieces)
    {
        StringBuilder board = new();
        for (int rank = ranks - 1; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < files; file++)
            {
                if (pieces.TryGetValue((file, rank), out char piece))
                {
                    _ = empty > 0 ? board.Append(empty) : board;
                    _ = board.Append(piece);
                    empty = 0;
                }
                else
                {
                    empty++;
                }
            }

            _ = empty > 0 ? board.Append(empty) : board;
            _ = rank > 0 ? board.Append('/') : board;
        }

        return $"{board} w - - 0 1";
    }
}
