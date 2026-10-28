using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The dice (rules.md §4, <c>dice.profile</c>): how many sides each has, and which kinds of piece each
/// value lets the roller move.
/// </summary>
public sealed record DiceProfile(int Sides, ImmutableDictionary<int, ImmutableHashSet<PieceKindId>> Kinds)
{
    /// <summary>Two ordinary six-sided dice, weighted toward the pieces there are more of: the default.</summary>
    public static DiceProfile WeightedSixSided { get; } = new(
        6,
        ImmutableDictionary<int, ImmutableHashSet<PieceKindId>>.Empty
            .Add(1, [Chaturaji2027Pieces.Boat])
            .Add(2, [Chaturaji2027Pieces.Horse])
            .Add(3, [Chaturaji2027Pieces.Elephant])
            .Add(4, [Chaturaji2027Pieces.Elephant])
            .Add(5, [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Raja])
            .Add(6, [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Raja])
    );

    /// <summary>Two four-sided dice, one value per kind: the original game's.</summary>
    public static DiceProfile FourSided { get; } = new(
        4,
        ImmutableDictionary<int, ImmutableHashSet<PieceKindId>>.Empty
            .Add(1, [Chaturaji2027Pieces.Boat])
            .Add(2, [Chaturaji2027Pieces.Horse])
            .Add(3, [Chaturaji2027Pieces.Elephant])
            .Add(4, [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Raja])
    );

    /// <summary>The kinds of piece <paramref name="value"/> lets move; none for a value no die shows.</summary>
    public ImmutableHashSet<PieceKindId> KindsOf(int value) => this.Kinds.GetValueOrDefault(value, []);
}
