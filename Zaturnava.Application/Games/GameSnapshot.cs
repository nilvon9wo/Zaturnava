using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>A game as it stands: its record, its rules, the replayed state, and what may be done next.</summary>
public sealed record GameSnapshot(
    GameRecord Record,
    IRulesApplying Rules,
    GameState State,
    ImmutableList<GameAction> LegalActions
)
{
    public static GameSnapshot Of(
        [Required] GameRecord record,
        [Required] IRulesApplying rules,
        [Required] GameState state
    ) =>
        new(record, rules, state, rules.LegalActions(state));

    /// <summary>Whether <paramref name="seat"/> is deciding now, and <paramref name="token"/> holds it.</summary>
    public bool IsDecidedBy(SeatId seat, [Required] string token) =>
        this.State.DecidingSeat == seat && this.Record.Controllers[seat.Value].IsHeldBy(token);
}
