using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Where a palace may stand in a kingdom, as seat 0 sees it (specification §5): a 3 × 3 of its squares, about a centre
/// whose 3 × 3 is all in the kingdom; and the one that holds a throne, wherever the throne is first set, so that
/// whoever works it out - the draft, or the program dealing an army - finds the same.
/// </summary>
public static class PalaceSquares
{
    /// <summary>Every square of <paramref name="kingdom"/> a palace may stand about: none, if it has no room.</summary>
    public static ImmutableList<ArmySquare> Centres([Required] ImmutableList<ArmySquare> kingdom)
    {
        ImmutableHashSet<ArmySquare> all = [.. kingdom];
        return [.. kingdom.Where(square => Window(square).All(all.Contains))];
    }

    /// <summary>
    /// The centre of the palace holding <paramref name="throne"/>: of <paramref name="centres"/>, the nearest it
    /// (Proposed), the lowest, then leftmost, of those as near.
    /// </summary>
    public static ArmySquare CentreAbout([Required] ImmutableList<ArmySquare> centres, [Required] ArmySquare throne) =>
        centres
            .OrderBy(centre => Math.Max(Math.Abs(centre.Column - throne.Column), Math.Abs(centre.Row - throne.Row)))
            .ThenBy(centre => centre.Row)
            .ThenBy(centre => centre.Column)
            .First();

    /// <summary>The nine squares of the palace about <paramref name="centre"/>, row by row from the back.</summary>
    public static ImmutableList<ArmySquare> Window([Required] ArmySquare centre) =>
    [
        .. Enumerable.Range(-1, 3).SelectMany(row =>
            Enumerable.Range(-1, 3).Select(column => new ArmySquare(centre.Column + column, centre.Row + row))
        ),
    ];
}
