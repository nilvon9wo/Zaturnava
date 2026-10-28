using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>How a game ended: the seats that won (none for a draw), and why, for people.</summary>
public sealed record GameOutcome(ImmutableList<SeatId> Winners, string Reason)
{
    public bool IsDraw => this.Winners.IsEmpty;

    public bool Equals(GameOutcome? other) =>
        other is not null && this.Reason == other.Reason && this.Winners.SequenceEqual(other.Winners);

    public override int GetHashCode() => HashCode.Combine(this.Reason, this.Winners.Count);
}
