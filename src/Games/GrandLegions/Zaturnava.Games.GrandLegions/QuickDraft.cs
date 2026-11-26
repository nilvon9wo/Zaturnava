using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A quick game's armies drafted by its players (specification §3; Proposed: in turn, in view, each palace placed
/// first): the game's budget each, any piece a quick army may have (<see cref="QuickArmy.Eligible"/>) at its price,
/// placed anywhere in the kingdom; and, in a game with palaces, the pieces a palace holds
/// (<see cref="PalaceConfinement.Confined"/>), placed in the player's own palace. An army filled at random from nothing
/// is one the program would deal; one partly drafted gains what it can still pay for, as <see cref="Filled"/> says: a
/// piece a palace holds only in a game whose armies may hold them (<see cref="ArmyFill.HasPalacePieces"/>), and only
/// while its palace has a free square.
/// </summary>
public static class QuickDraft
{
    /// <summary>
    /// The edition (<see cref="ArmyFill.Edition"/>) from which an army filled at random from nothing keeps the palace
    /// already placed (<see cref="QuickArmy.Layout"/>); a game dealt before replays as it was dealt.
    /// </summary>
    public const int PalaceKeptEdition = QuickArmy.ThroneKeptEdition;

    public static Drafting Of([Required] QuickGame game) =>
        new(
            game.Budget,
            game.Kingdom,
            (terrain, edition) =>
                [.. QuickArmy.Eligible(GameOn(game, terrain) with { Edition = edition }).Select(piece => piece.Id)],
            fill => Filled(game, fill)
        )
        {
            Terrains = [.. Enumerable.Range(0, (int)Rivers.Both + 1)],
            PalaceBound = PalaceConfinement.Confined,
            BudgetAt = edition => QuickArmy.BudgetOf(game with { Edition = edition }),
            LiftPrice = LiftPriceOf,
        };

    /// <summary>
    /// What buying off a kind's restriction costs: the catalogue's lift, of its movement restriction or its area
    /// restriction (<see cref="LiftedMoves"/>); none, for a kind without one.
    /// </summary>
    public static int? LiftPriceOf([Required] PieceKindId kind, LiftKind restriction) =>
        GrandLegionsPieces.ById[kind].Restrictions
            .FirstOrDefault(each =>
                each.Kind == (restriction == LiftKind.Area ? RestrictionKind.Area : RestrictionKind.Movement)
            )
            ?.Lift;

    /// <summary>
    /// The rest of an army, drafted at random. From nothing (a throne is set before any piece), as the program deals
    /// an army, its leader's square its throne. Else, while it can pay for one and has a free square, a piece at
    /// random, as the program deals the pieces past
    /// each class's least: first a leader, on its throne, if it has none and the throne is free; never another;
    /// a piece a palace holds on a free square of its palace, but the throne; infantry on the free square furthest
    /// forward, the rest on the one furthest back.
    /// </summary>
    public static ArmyFilled Filled([Required] QuickGame game, [Required] ArmyFill fill)
    {
        QuickGame played = GameOn(game, fill.Terrain) with
        {
            Budget = fill.Left,
            HasPalacePieces = fill.HasPalacePieces,
            HasEdgePalaces = fill.HasEdgePalaces,
            Edition = fill.Edition,
        };
        if (fill.Throne is not { } throne)
        {
            ImmutableList<ArmySquare>? placed = fill.Edition >= PalaceKeptEdition ? fill.Palace : null;
            ImmutableList<ArmyPiece> dealt =
                QuickArmy.Layout(played, QuickArmy.Pick(played, fill.Random), fill.Random, placed);
            ArmyPiece leader = dealt.First(piece => GrandLegionsPieces.ById[piece.Kind].Class == Leader);
            return new ArmyFilled(dealt, new ArmySquare(leader.Column, leader.Row));
        }

        return new ArmyFilled(Added(played, QuickArmy.Eligible(played), fill, throne, []), throne);
    }

    private static QuickGame GameOn(QuickGame game, int terrain) =>
        game with { Rivers = QuickTerrain.RiversOf(terrain) };

    /// <summary>
    /// The pieces added at random, after <paramref name="added"/>, while any fits: from
    /// <see cref="QuickArmy.CompositionEdition"/> on, each the piece <see cref="QuickArmy.Next"/> gives the army
    /// toward the composition it aims at, a leader too while the army has none and its throne is free, or once it has
    /// one; before, any piece it can pay for, a leader first.
    /// </summary>
    private static ImmutableList<ArmyPiece> Added(
        QuickGame game,
        ImmutableList<GrandPiece> eligible,
        ArmyFill fill,
        ArmySquare throne,
        ImmutableList<ArmyPiece> added
    )
    {
        ImmutableList<ArmySquare> kingdom = game.Kingdom;
        ImmutableList<ArmyPiece> army = fill.Army.AddRange(added);
        ImmutableHashSet<ArmySquare> taken = [.. army.Select(piece => new ArmySquare(piece.Column, piece.Row))];
        ImmutableList<ArmySquare> free = [.. kingdom.Where(square => !taken.Contains(square))];
        ImmutableList<ArmySquare> palace =
        [
            .. (fill.Palace ?? [])
                .Where(square => square != throne && !taken.Contains(square))
                .OrderBy(square => square.Row)
                .ThenBy(square => Math.Abs(square.Column - throne.Column))
                .ThenBy(square => square.Column),
        ];
        bool hasLeader = army.Any(piece => GrandLegionsPieces.ById[piece.Kind].Class == Leader);
        int left = fill.Left - added.Sum(piece => eligible.First(each => each.Id == piece.Kind).Value);
        bool isComposed = fill.Edition >= QuickArmy.CompositionEdition;
        ImmutableList<GrandPiece> placeable =
        [
            .. eligible
                .Where(piece =>
                    piece.Class != Leader || (isComposed && hasLeader) || (!hasLeader && !taken.Contains(throne))
                )
                .Where(piece => !QuickArmy.IsHeld(piece, fill.Edition, hasLeader) || !palace.IsEmpty),
        ];
        GrandPiece? piece = free.IsEmpty
            ? null
            : isComposed
                ? QuickArmy.Next(
                    game,
                    placeable,
                    [.. army.Select(each => GrandLegionsPieces.ById[each.Kind])],
                    left,
                    fill.Random
                )
                : LeaderFirst(placeable.Where(each => each.Value <= left), fill.Random);
        if (piece is null)
        {
            return added;
        }

        ArmySquare square =
            QuickArmy.IsHeld(piece, fill.Edition, hasLeader) ? palace[0] : SquareFor(piece, free, throne);
        return Added(game, eligible, fill, throne, added.Add(new ArmyPiece(piece.Id, square.Column, square.Row)));
    }

    /// <summary>
    /// A piece among <paramref name="affordable"/>, at random, a leader first, if one is among them; none, if none is.
    /// </summary>
    private static GrandPiece? LeaderFirst(IEnumerable<GrandPiece> affordable, Random random)
    {
        ImmutableList<GrandPiece> choices = [.. affordable];
        ImmutableList<GrandPiece> leaders = [.. choices.Where(each => each.Class == Leader)];
        return choices.IsEmpty ? null : Drawn(leaders.IsEmpty ? choices : leaders, random);
    }

    /// <summary>
    /// Where a piece drawn goes, if not one a palace holds: a leader on the throne, while it is free; infantry on the
    /// free square furthest forward; any other on the free square furthest back, sparing the throne while another is
    /// free.
    /// </summary>
    private static ArmySquare SquareFor(GrandPiece piece, ImmutableList<ArmySquare> free, ArmySquare throne) =>
        piece.Class switch
        {
            Leader when free.Contains(throne) => throne,
            Infantry => free.OrderByDescending(square => square.Row).ThenBy(square => square.Column).First(),
            _ => free.OrderBy(square => square == throne).ThenBy(square => square.Row).ThenBy(square => square.Column)
                .First(),
        };

    private static T Drawn<T>(ImmutableList<T> choices, Random random) => choices[random.Next(choices.Count)];
}
