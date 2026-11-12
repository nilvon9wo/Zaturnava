using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// The armies being drafted, before play begins: what each seat has left to spend (<paramref name="Left"/>), the
/// seats done drafting (<paramref name="Done"/>), and the seed the game was dealt by, which any army filled at random
/// is drafted by, so that the game replays alike.
/// </summary>
public sealed record ArmyDraft(int Seed, ImmutableDictionary<SeatId, int> Left, ImmutableHashSet<SeatId> Done);
