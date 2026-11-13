using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// How a game's creator set its armies' draft (Stated, 2026-11-12: settings of the game, not rules): what every seat
/// has to spend, one value for everyone (<paramref name="Budget"/>; the ruleset's own, if none); and, if
/// <paramref name="Drafter"/> names a seat, that seat alone drafts, one army that every seat then has alike.
/// </summary>
public sealed record DraftSettings(int? Budget = null, SeatId? Drafter = null)
{
    /// <summary>
    /// Whether each seat drafts in secret (Stated, 2026-11-13: the user asked that drafting in secret be possible): no
    /// one sees another team's palace, throne, pieces or points left until every army is drafted (Proposed). The
    /// rules play alike either way; only what each seat is shown differs.
    /// </summary>
    public bool IsSecret { get; init; }

    /// <summary>Every seat drafts its own army, with the ruleset's budget.</summary>
    public static DraftSettings Default { get; } = new();
}
