using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// A leader face-off a seat a person plays may call, first open after the game's action at <paramref name="Index"/>
/// (Grand Legions specification §6.4, Stated: a setting of the game may pause it when the chance first comes), so
/// that the game may pause for them (<see cref="FaceOffPause"/>).
/// </summary>
public sealed record FaceOffOpening(SeatId Seat, int Index)
{
    /// <summary>
    /// The opening as it stands after the action at <paramref name="index"/> took the game from
    /// <paramref name="before"/> to <paramref name="after"/>: one it opened, if it opened one; else
    /// <paramref name="current"/>, while its seat may still call a face-off.
    /// </summary>
    public static FaceOffOpening? Next(
        [Required] IRulesApplying rules,
        [Required] ImmutableList<SeatController> controllers,
        FaceOffOpening? current,
        [Required] GameState before,
        [Required] GameState after,
        int index
    ) =>
        Opened(rules, controllers, before, after) is { } seat
            ? new FaceOffOpening(seat, index)
            : current is { } open && !FaceOffsOf(rules, after, open.Seat).IsEmpty
                ? current
                : null;

    /// <summary>
    /// The first seat a person plays that may call a face-off <paramref name="after"/> that it could not
    /// <paramref name="before"/>; none, if none may.
    /// </summary>
    public static SeatId? Opened(
        [Required] IRulesApplying rules,
        [Required] ImmutableList<SeatController> controllers,
        [Required] GameState before,
        [Required] GameState after
    ) =>
        after.Seats
            .Select(seat => seat.Id)
            .Where(seat => controllers[seat.Value] is HumanSeat)
            .Select(seat => (SeatId?)seat)
            .FirstOrDefault(seat => !FaceOffsOf(rules, after, seat!.Value)
                .Except(FaceOffsOf(rules, before, seat.Value))
                .IsEmpty
            );

    private static ImmutableHashSet<GameAction> FaceOffsOf(IRulesApplying rules, GameState state, SeatId seat) =>
        [.. rules.ChancesOf(state, seat).OfType<FaceOffAction>()];
}
