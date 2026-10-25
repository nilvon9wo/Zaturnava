using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Engine.Chance;

/// <summary>
/// Chance, handed in: real dice, a seeded generator for a replayable test, or the rolls a recorded
/// game actually had. The engine itself never rolls.
/// </summary>
public interface IDiceRolling
{
    /// <summary>One value from 1 to <see cref="DiceRequest.Sides"/> for each of the requested dice.</summary>
    ImmutableList<int> Roll([Required] DiceRequest request);
}
