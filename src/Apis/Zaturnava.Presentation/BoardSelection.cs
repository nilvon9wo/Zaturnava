using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// The cell a player has picked up a piece from, if any, and what each click on the board does:
/// pick a piece up, put it down (which chooses an action), or pick up another instead; with nothing
/// picked up, a click on a cell something may be dropped on chooses that.
/// </summary>
public sealed record BoardSelection(int? Selected)
{
    public static readonly BoardSelection None = new((int?)null);

    /// <summary>The selection after a click on <paramref name="cell"/>, and the action it chose, if any.</summary>
    public (BoardSelection Next, ActionDto? Chosen) Click(int cell, [Required] BoardActions actions) =>
        this.Selected switch
        {
            { } from when actions.Between(from, cell) is { } chosen => (None, chosen),
            null when actions.DropAt(cell) is { } dropped => (None, dropped),
            _ => (actions.Origins.Contains(cell) && this.Selected != cell ? new BoardSelection(cell) : None, null),
        };
}
