using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// The cell a player has picked up a piece from, if any, or what they chose to place (<see cref="Choice"/>, as a
/// prisoner to escape), and what each click on the board does: pick a piece up, put it down (which chooses an
/// action), or pick up another instead; with nothing picked up, a click on a cell something may be dropped on
/// chooses that. With a choice made, a click on a cell it may be placed on chooses that; a click anywhere else
/// lets the choice go, and does what it would have done without it.
/// </summary>
public sealed record BoardSelection(int? Selected, string? Choice = null)
{
    public static readonly BoardSelection None = new((int?)null);

    /// <summary>Nothing picked up, and <paramref name="choice"/> to place, by a click on a cell.</summary>
    public static BoardSelection Choosing(string choice) => new(null, choice);

    /// <summary>The selection after a click on <paramref name="cell"/>, and the action it chose, if any.</summary>
    public (BoardSelection Next, ActionDto? Chosen) Click(int cell, [Required] BoardActions actions) =>
        (this.Choice, this.Selected) switch
        {
            ({ } choice, _) when actions.PlacementAt(choice, cell) is { } placed => (None, placed),
            ({ }, _) => None.Click(cell, actions),
            (_, { } from) when actions.Between(from, cell) is { } chosen => (None, chosen),
            (_, null) when actions.DropAt(cell) is { } dropped => (None, dropped),
            _ => (actions.Origins.Contains(cell) && this.Selected != cell ? new BoardSelection(cell) : None, null),
        };
}
