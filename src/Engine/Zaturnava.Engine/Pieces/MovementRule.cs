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
    /// <summary>One step, moving or capturing.</summary>
    public static MovementRule Step([Required] DirectionSet directions) =>
        new(directions, 1, 1, false, true, true);

    /// <summary>Any number of steps, stopped by the first piece in the way.</summary>
    public static MovementRule Slide([Required] DirectionSet directions) =>
        new(directions, 1, int.MaxValue, false, true, true);

    /// <summary>Exactly <paramref name="distance"/> steps, over whatever is in between.</summary>
    public static MovementRule Leap([Required] DirectionSet directions, [Range(1, int.MaxValue)] int distance) =>
        new(directions, distance, distance, true, true, true);

    public MovementRule MoveOnly() => this with { CanCapture = false };

    public MovementRule CaptureOnly() => this with { CanMove = false };
}
