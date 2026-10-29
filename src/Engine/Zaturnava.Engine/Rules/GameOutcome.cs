using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// How a game ended: the seats that won (none for a draw), the kind of ending as a code the ruleset
/// names (one team left, an agreement...), for programs learning from games, and why, for people.
/// </summary>
public sealed record GameOutcome(ImmutableList<SeatId> Winners, string Ending, string Reason)
{
    public bool IsDraw => this.Winners.IsEmpty;

    public bool Equals(GameOutcome? other) =>
        other is not null
        && this.Ending == other.Ending
        && this.Reason == other.Reason
        && this.Winners.SequenceEqual(other.Winners);

    public override int GetHashCode() => HashCode.Combine(this.Ending, this.Reason, this.Winners.Count);
}
