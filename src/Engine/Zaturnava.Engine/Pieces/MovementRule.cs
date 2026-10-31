using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// One clause of the movement grammar: travel along each direction of <see cref="Directions"/>
/// between <see cref="MinimumDistance"/> and <see cref="MaximumDistance"/> steps, either blocked by
/// whatever is in the way or leaping over it, onto an empty cell, a capture, or either.
/// </summary>
public sealed record MovementRule(
    DirectionSet Directions,
    int MinimumDistance,
    int MaximumDistance,
    bool IsLeaping,
    bool CanMove,
    bool CanCapture
)
{
    /// <summary>
    /// Whether each step is a lame leap: blocked by a piece on any cell it passes on the way (the topology's
    /// <see cref="ICellNavigating.WaypointsOf"/>), as the Xiangqi Horse's leg is.
    /// </summary>
    public bool IsLame { get; init; }

    /// <summary>
    /// Whether it hops: it must pass over exactly one piece on its line (the screen), and reaches only the cells
    /// beyond it, up to <see cref="MaximumBeyond"/> of them, and no further than the next piece.
    /// </summary>
    public bool IsHopping { get; init; }

    /// <summary>How many cells beyond the screen a hop may land on: any number, unless limited.</summary>
    public int MaximumBeyond { get; init; } = int.MaxValue;

    /// <summary>The kinds it may not hop over, nor capture: as a Janggi Cannon may not, a Cannon.</summary>
    public ImmutableHashSet<PieceKindId> Shunned { get; init; } = [];

    /// <summary>One step, moving or capturing.</summary>
    public static MovementRule Step([Required] DirectionSet directions) =>
        new(directions, 1, 1, false, true, true);

    /// <summary>Any number of steps, stopped by the first piece in the way.</summary>
    public static MovementRule Slide([Required] DirectionSet directions) =>
        new(directions, 1, int.MaxValue, false, true, true);

    /// <summary>Exactly <paramref name="distance"/> steps, over whatever is in between.</summary>
    public static MovementRule Leap([Required] DirectionSet directions, [Range(1, int.MaxValue)] int distance) =>
        new(directions, distance, distance, true, true, true);

    /// <summary>
    /// Along each line, over the first piece met, to any cell beyond it, up to the next piece (capturing it);
    /// or only up to <paramref name="beyond"/> cells beyond.
    /// </summary>
    public static MovementRule Hop([Required] DirectionSet directions, [Range(1, int.MaxValue)] int beyond) =>
        Slide(directions) with { IsHopping = true, MaximumBeyond = beyond };

    public MovementRule MoveOnly() => this with { CanCapture = false };

    public MovementRule CaptureOnly() => this with { CanMove = false };

    /// <summary>The same, but blocked by a piece on any cell a step passes on the way.</summary>
    public MovementRule Lame() => this with { IsLame = true };

    /// <summary>The same, but never hopping over, nor capturing, a piece of <paramref name="kinds"/>.</summary>
    public MovementRule Shunning([Required] IEnumerable<PieceKindId> kinds) =>
        this with { Shunned = [.. kinds] };
}
