using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// A game's legal actions, sorted for a player: those drawn on the board (they go from one cell to
/// another, see <see cref="ActionArguments"/>); those taken at a cell, offered once its piece is
/// picked up; and the rest, offered as buttons. Nothing here knows what any kind of action means.
/// </summary>
public sealed class BoardActions([Required] ImmutableList<ActionDto> legal)
{
    private readonly ImmutableList<(int From, int To, ActionDto Action)> _moves = [.. legal.SelectMany(MoveOf)];
    private readonly ImmutableList<(int Cell, ActionDto Action)> _atCells = [.. legal.SelectMany(AtCellOf)];

    /// <summary>The actions that are neither drawn on the board nor taken at a cell.</summary>
    public ImmutableList<ActionDto> OffBoard { get; } =
        [.. legal.Where(action => CellsOf(action) is null && CellOf(action, ActionArguments.Cell) is null)];

    /// <summary>The cells some action on the board starts from, or is taken at.</summary>
    public ImmutableHashSet<int> Origins => [.. this._moves.Select(move => move.From), .. this.AtCells];

    /// <summary>The cells some action is taken at, as a promotion is.</summary>
    public ImmutableHashSet<int> AtCells => [.. this._atCells.Select(atCell => atCell.Cell)];

    /// <summary>The actions taken at <paramref name="cell"/>, in the order they came.</summary>
    public ImmutableList<ActionDto> At(int cell) =>
        [.. this._atCells.Where(atCell => atCell.Cell == cell).Select(atCell => atCell.Action)];

    /// <summary>The cells an action from <paramref name="from"/> can go to.</summary>
    public ImmutableHashSet<int> TargetsFrom(int from) =>
        [.. this._moves.Where(move => move.From == from).Select(move => move.To)];

    /// <summary>The action from <paramref name="from"/> to <paramref name="to"/>, if there is one.</summary>
    public ActionDto? Between(int from, int to) =>
        this._moves.Where(move => move.From == from && move.To == to).Select(move => move.Action).FirstOrDefault();

    private static IEnumerable<(int From, int To, ActionDto Action)> MoveOf(ActionDto action) =>
        CellsOf(action) is { } cells ? [(cells.From, cells.To, action)] : [];

    private static IEnumerable<(int Cell, ActionDto Action)> AtCellOf(ActionDto action) =>
        CellOf(action, ActionArguments.Cell) is { } cell ? [(cell, action)] : [];

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
