using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary><see cref="Rulers"/>, a team, no longer rule an empire.</summary>
public sealed record EmpireEnded(ImmutableList<SeatId> Rulers) : GameEvent
{
    public bool Equals(EmpireEnded? other) => other is not null && this.Rulers.SequenceEqual(other.Rulers);

    public override int GetHashCode() => this.Rulers.Count;
}
