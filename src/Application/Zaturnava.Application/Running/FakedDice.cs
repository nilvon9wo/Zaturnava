using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// Dice that come up as <paramref name="faces"/> say the first time they are rolled, each face not given rolled by
/// <paramref name="dice"/>; then as <paramref name="dice"/> rolls them (the user's request of 2026-11-19: in a
/// scenario, to "roll" a double 6, say, to test a return).
/// </summary>
public sealed class FakedDice(IDiceRolling dice, ImmutableList<int?> faces) : IDiceRolling
{
    private readonly IDiceRolling _dice = dice;
    private ImmutableList<int?>? _faces = faces;

    public ImmutableList<int> Roll([Required] DiceRequest request)
    {
        ImmutableList<int> rolled = this._dice.Roll(request);
        ImmutableList<int?> faces = this._faces ?? [];
        this._faces = null;
        return [.. rolled.Select((value, index) => faces.ElementAtOrDefault(index) ?? value)];
    }
}
