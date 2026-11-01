using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// Grand Legions played by Chaturaji 2027's rules (Proposed): each leader is what those rules call a Raja, its
/// start its seat's throne; the Western, Makruk and Shogi Pawns are their Pawns, promoted at the far edge into a
/// captured piece; a die names a class, so moves any piece of it; and taking a piece scores its value. Palaces,
/// more than one leader, and each piece's own promotion are still to come.
/// </summary>
public static class GrandLegionsRules
{
    public static ImmutableHashSet<PieceKindId> Pawns { get; } =
        [GrandLegionsPieces.WesternPawn, GrandLegionsPieces.MakrukPawn, GrandLegionsPieces.ShogiPawn];

    public static ImmutableHashSet<PieceKindId> Leaders { get; } =
    [
        .. GrandLegionsPieces.Catalogue
            .Where(piece => piece.Class == PieceClass.Leader)
            .Select(piece => piece.Id),
    ];

    public static ImmutableHashSet<PieceKindId> Heavies { get; } =
    [
        .. GrandLegionsPieces.Catalogue
            .Where(piece => piece.Class == PieceClass.Heavy)
            .Select(piece => piece.Id),
    ];

    /// <summary>The rules of each quick game, small, medium and large.</summary>
    public static ImmutableList<Chaturaji2027Rules> Quick { get; } = [.. QuickGame.All.Select(RulesOf)];

    public static Chaturaji2027Rules RulesOf([Required] QuickGame game) =>
        new(
            DiceOf(ClassDice.SixSided),
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            Chaturaji2027Rules.DefaultAwayTurns,
            VariantOf(game, ClassDice.SixSided)
        );

    /// <summary>The quick game as the rules play it: its board, its armies as the program picks them.</summary>
    public static Chaturaji2027Variant VariantOf([Required] QuickGame game, [Required] ClassDice dice) =>
        new(
            game.Id,
            game.Name,
            game.Side,
            GrandLegionsPieces.Kinds,
            GrandLegionsPieces.Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Value),
            GrandLegionsPieces.Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Name),
            Leaders,
            Pawns,
            Pinwheel.Armies(game.Side, QuickArmy.Layout(game, QuickArmy.Pick(game)))
        )
        {
            Heavies = Heavies,
            DieNames = dice.Classes.ToImmutableDictionary(face => face.Key, face => NameOf(face.Value)),
            MayMove = RiverCrossing.On(new GrandBoard(game.Side, game.Side, game.KingdomWidth, game.KingdomDepth)),
        };

    /// <summary>The dice as kinds: each value lets every piece of the classes it names move.</summary>
    public static DiceProfile DiceOf([Required] ClassDice dice) =>
        new(
            dice.Sides,
            dice.Classes.ToImmutableDictionary(
                face => face.Key,
                face => GrandLegionsPieces.Catalogue
                    .Where(piece => face.Value.Contains(piece.Class))
                    .Select(piece => piece.Id)
                    .ToImmutableHashSet()
            )
        );

    /// <summary>"Infantry or Leader".</summary>
    private static string NameOf(ImmutableHashSet<PieceClass> classes) =>
        string.Join(" or ", classes.Select(pieceClass => pieceClass.ToString()).Order(StringComparer.Ordinal));
}
