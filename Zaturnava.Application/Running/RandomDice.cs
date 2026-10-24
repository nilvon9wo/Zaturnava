using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// Fair dice, from <paramref name="random"/>: <see cref="Random.Shared"/>, or a seeded one for a test.
/// </summary>
public sealed class RandomDice(Random random) : IDiceRolling
{
    private readonly Random _random = random;

    public ImmutableList<int> Roll([Required] DiceRequest request) =>
        [.. Enumerable.Range(0, request.Count).Select(_ => this._random.Next(1, request.Sides + 1))];
}
