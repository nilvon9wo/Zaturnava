using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>Dice that always come up <paramref name="value"/>.</summary>
internal sealed class LoadedDice(int value) : IDiceRolling
{
    private readonly int _value = value;

    public ImmutableList<int> Roll([Required] DiceRequest request) =>
        [.. Enumerable.Repeat(this._value, request.Count)];
}
