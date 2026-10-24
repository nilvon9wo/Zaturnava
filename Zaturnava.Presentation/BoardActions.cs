using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// A game's legal actions, sorted for a player: those drawn on the board (they go from one cell to
/// another, see <see cref="ActionArguments"/>), and the rest, offered as buttons. Nothing here knows
/// what any kind of action means.
/// </summary>
public sealed class BoardActions([Required] ImmutableList<ActionDto> legal)
{
    private readonly ImmutableList<(int From, int To, ActionDto Action)> _moves = [.. legal.SelectMany(MoveOf)];

    /// <summary>The actions that are not drawn on the board.</summary>
    public ImmutableList<ActionDto> OffBoard { get; } = [.. legal.Where(action => CellsOf(action) is null)];

    /// <summary>The cells some action on the board starts from.</summary>
    public ImmutableHashSet<int> Origins => [.. this._moves.Select(move => move.From)];

    /// <summary>The cells an action from <paramref name="from"/> can go to.</summary>
    public ImmutableHashSet<int> TargetsFrom(int from) =>
        [.. this._moves.Where(move => move.From == from).Select(move => move.To)];

    /// <summary>The action from <paramref name="from"/> to <paramref name="to"/>, if there is one.</summary>
    public ActionDto? Between(int from, int to) =>
        this._moves.Where(move => move.From == from && move.To == to).Select(move => move.Action).FirstOrDefault();

    private static IEnumerable<(int From, int To, ActionDto Action)> MoveOf(ActionDto action) =>
        CellsOf(action) is { } cells ? [(cells.From, cells.To, action)] : [];

    private static (int From, int To)? CellsOf(ActionDto action) =>
        CellOf(action, ActionArguments.From) is { } from && CellOf(action, ActionArguments.To) is { } to
            ? (from, to)
            : null;

    private static int? CellOf(ActionDto action, string argument) =>
        action.Arguments.TryGetValue(argument, out string? text)
        && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int cell)
            ? cell
            : null;
}
