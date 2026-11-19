using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Cells of a drawing marked alike (<see cref="MoveDiagram.Marks"/>): <paramref name="Mark"/>, how - where it lands
/// (<see cref="Lands"/>), a piece taken (<see cref="Taken"/>), a piece standing there (<see cref="Standing"/>), the
/// way a piece went (<see cref="Trail"/>), or a piece won over (<see cref="Converted"/>) - and
/// <paramref name="Meaning"/>, what the mark means in this drawing.
/// </summary>
public sealed record CellMark(string Mark, string Meaning, ImmutableList<int> Cells)
{
    public const string Lands = "move";
    public const string Taken = "taken capture";
    public const string Standing = "partner";
    public const string Trail = "trail";
    public const string Converted = "converted";

    /// <summary>A piece of its own side, taken, as a piece gone berserk takes one.</summary>
    public const string TakenFriend = "taken capture friend";

    /// <summary>An enemy standing there, as one holding a piece with a piece gone berserk.</summary>
    public const string StandingFoe = "partner foe";

    /// <summary>An enemy standing on the piece's own throne.</summary>
    public const string OnThrone = "partner foe on-throne";
}
