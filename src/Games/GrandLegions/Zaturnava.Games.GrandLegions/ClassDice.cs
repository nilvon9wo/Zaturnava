using System.Collections.Immutable;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The dice (specification §6.1): how many sides each has, and which classes of piece each value lets the
/// roller move. A die names a class, not a kind: any piece of that class may move.
/// </summary>
public sealed record ClassDice(int Sides, ImmutableDictionary<int, ImmutableHashSet<PieceClass>> Classes)
{
    /// <summary>Two six-sided dice, mapped as Chaturaji 2027's faces are: the default.</summary>
    public static ClassDice SixSided { get; } = new(
        6,
        ImmutableDictionary<int, ImmutableHashSet<PieceClass>>.Empty
            .Add(1, [Artillery])
            .Add(2, [Cavalry])
            .Add(3, [Heavy])
            .Add(4, [Heavy])
            .Add(5, [Infantry, Leader])
            .Add(6, [Infantry, Leader])
    );

    /// <summary>The classes <paramref name="value"/> lets move; none for a value no die shows.</summary>
    public ImmutableHashSet<PieceClass> ClassesOf(int value) => this.Classes.GetValueOrDefault(value, []);
}
