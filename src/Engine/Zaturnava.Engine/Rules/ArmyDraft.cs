using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// The armies being drafted, before play begins: what each seat has left to spend (<paramref name="Left"/>), the
/// seats done drafting (<paramref name="Done"/>), and the seed the game was dealt by, which any army filled at random
/// is drafted by, so that the game replays alike.
/// </summary>
public sealed record ArmyDraft(int Seed, ImmutableDictionary<SeatId, int> Left, ImmutableHashSet<SeatId> Done)
{
    /// <summary>
    /// The seat that alone drafts, one army for every seat, which each has alike once it is done
    /// (<see cref="DraftSettings.Drafter"/>); none, if each drafts its own.
    /// </summary>
    public SeatId? Drafter { get; init; }

    /// <summary>
    /// Whether the seats draft all at once, not in turn (<see cref="DraftSettings.IsSimultaneous"/>): any seat not
    /// done may take its next step whenever it likes, take back a piece it drafted, or move its throne or palace.
    /// </summary>
    public bool IsSimultaneous { get; init; }
}
