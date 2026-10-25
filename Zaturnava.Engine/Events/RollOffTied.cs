using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Seats"/> rolled highest together, and roll again.</summary>
public sealed record RollOffTied(ImmutableList<SeatId> Seats) : GameEvent
{
    public bool Equals(RollOffTied? other) => other is not null && this.Seats.SequenceEqual(other.Seats);

    public override int GetHashCode() => this.Seats.Count;
}
