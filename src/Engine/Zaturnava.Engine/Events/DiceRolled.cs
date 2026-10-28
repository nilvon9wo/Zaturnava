using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seat"/>'s dice came up <see cref="Values"/>, in that order.</summary>
public sealed record DiceRolled(SeatId Seat, ImmutableList<int> Values) : GameEvent
{
    public bool Equals(DiceRolled? other) =>
        other is not null && this.Seat == other.Seat && this.Values.SequenceEqual(other.Values);

    public override int GetHashCode() => HashCode.Combine(this.Seat, this.Values.Count);
}
