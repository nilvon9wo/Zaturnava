using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
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
        };

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
            ImmutableList<ArmyPiece> dealt = QuickArmy.Layout(played, QuickArmy.Pick(played, fill.Random), fill.Random);
            ArmyPiece leader = dealt.First(piece => GrandLegionsPieces.ById[piece.Kind].Class == Leader);
            return new ArmyFilled(dealt, new ArmySquare(leader.Column, leader.Row));
        }

        return new ArmyFilled(Added(QuickArmy.Eligible(played), fill, Of(game).Kingdom, throne, []), throne);
    }

    private static QuickGame GameOn(QuickGame game, int terrain) =>
        game with { Rivers = QuickTerrain.RiversOf(terrain) };

    /// <summary>The pieces added at random, after <paramref name="added"/>, while any fits.</summary>
    private static ImmutableList<ArmyPiece> Added(
        ImmutableList<GrandPiece> eligible,
        ArmyFill fill,
        ImmutableList<ArmySquare> kingdom,
        ArmySquare throne,
        ImmutableList<ArmyPiece> added
    )
    {
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
        ImmutableList<GrandPiece> affordable =
        [
            .. eligible
                .Where(piece => piece.Value <= left)
                .Where(piece => piece.Class != Leader || (!hasLeader && !taken.Contains(throne)))
                .Where(piece => !QuickArmy.IsHeld(piece) || !palace.IsEmpty),
        ];
        if (affordable.IsEmpty || free.IsEmpty)
        {
            return added;
        }

        ImmutableList<GrandPiece> leaders = [.. affordable.Where(each => each.Class == Leader)];
        GrandPiece piece = Drawn(leaders.IsEmpty ? affordable : leaders, fill.Random);
        ArmySquare square = QuickArmy.IsHeld(piece) ? palace[0] : SquareFor(piece, free, throne);
        return Added(eligible, fill, kingdom, throne, added.Add(new ArmyPiece(piece.Id, square.Column, square.Row)));
    }

    /// <summary>
    /// Where a piece drawn goes, if not one a palace holds: a leader on the throne; infantry on the free square
    /// furthest forward; any other on the free square furthest back, sparing the throne while another is free.
    /// </summary>
    private static ArmySquare SquareFor(GrandPiece piece, ImmutableList<ArmySquare> free, ArmySquare throne) =>
        piece.Class switch
        {
            Leader => throne,
            Infantry => free.OrderByDescending(square => square.Row).ThenBy(square => square.Column).First(),
            _ => free.OrderBy(square => square == throne).ThenBy(square => square.Row).ThenBy(square => square.Column)
                .First(),
        };

    private static T Drawn<T>(ImmutableList<T> choices, Random random) => choices[random.Next(choices.Count)];
}
