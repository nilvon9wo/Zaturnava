using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Events;

/// <summary>
/// <see cref="Rulers"/>, a team, now rule an empire: no enemy leader is on the board, and one of them commands
/// an ally's army.
/// </summary>
public sealed record EmpireBegun(ImmutableList<SeatId> Rulers) : GameEvent
{
    public bool Equals(EmpireBegun? other) => other is not null && this.Rulers.SequenceEqual(other.Rulers);

    public override int GetHashCode() => this.Rulers.Count;
}
