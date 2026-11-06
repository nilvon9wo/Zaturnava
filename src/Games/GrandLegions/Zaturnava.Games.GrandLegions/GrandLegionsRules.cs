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

    /// <summary>
    /// What a player is told of each piece of the catalogue beyond its moves: its class, what its own promotion may
    /// make it - not played yet - and what holds it back. How much lifting a restriction costs is not told: quick
    /// games lift none.
    /// </summary>
    public static ImmutableDictionary<PieceKindId, PieceGuide> Guides { get; } =
        GrandLegionsPieces.Catalogue.ToImmutableDictionary(
            piece => piece.Id,
            piece => new PieceGuide(GrandLegionsPieces.Kinds[piece.Id])
            {
                Class = piece.Class.ToString(),
                PromotesTo = [.. piece.PromotesTo],
                Restrictions = [.. piece.Restrictions.Select(restriction => restriction.Description)],
            }
        );

    /// <summary>
    /// What the Chaturaji 2027 bonuses score in Grand Legions (Stated: each × 10, beside captures worth up to 75;
    /// the clear lead to win by giving up, × 10 too, Proposed), as its table has it.
    /// </summary>
    public static Scoring Scoring { get; } = Scoring.Of(typeof(GrandLegionsRules).Assembly, QuickGame.GameId);

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

    /// <summary>
    /// The quick game as the rules play it: its board, and its armies as the program deals them, each game its own;
    /// a game begun before games were dealt starts as the first seed deals it.
    /// </summary>
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
            QuickArmy.Dealt(game, 0)
        )
        {
            Dealer = seed => QuickArmy.Dealt(game, seed),
            Game = game.Game,
            Heavies = Heavies,
            Marks = GrandLegionsPieces.Marks,
            Guides = Guides,
            Scoring = Scoring,
            StartRowPowers = [GrandLegionsPieces.WesternPawn],
            Castlers = [GrandLegionsPieces.WesternKing],
            CastlingRooks = [GrandLegionsPieces.Rook],
            FlyingCapturers = [GrandLegionsPieces.XiangqiGeneral],
            FaceOffCallers = [GrandLegionsPieces.JanggiGeneral],
            DieNames = dice.Classes.ToImmutableDictionary(face => face.Key, face => NameOf(face.Value)),
            MayMove = RiverCrossing.On(
                new GrandBoard(game.Side, game.Side, game.KingdomWidth, game.KingdomDepth, Rivers: game.Rivers)
            ),
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
