using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/> could use none of the dice <see cref="Values"/> it had left, so lost them.</summary>
public sealed record DiceForfeited(SeatId Seat, ImmutableList<int> Values) : GameEvent
{
    public bool Equals(DiceForfeited? other) =>
        other is not null && this.Seat == other.Seat && this.Values.SequenceEqual(other.Values);

    public override int GetHashCode() => HashCode.Combine(this.Seat, this.Values.Count);
}
