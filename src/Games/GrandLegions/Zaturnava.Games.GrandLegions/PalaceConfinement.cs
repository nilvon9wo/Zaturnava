using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// What a palace means to the pieces confined to one (specification §5): such a piece in a palace stays in that
/// palace, whosever it is. One outside every palace moves freely until it enters one, which then holds it. Only the
/// Xiangqi General's flying-general capture takes it out (§6.4): onto a leader, wherever that stands. No piece
/// buys its confinement off yet, as no lifts are bought in the game yet.
/// </summary>
public static class PalaceConfinement
{
    /// <summary>The kinds a palace holds: those with a restriction keeping them in it.</summary>
    public static ImmutableHashSet<PieceKindId> Confined { get; } =
    [
        .. GrandLegionsPieces.Catalogue
            .Where(piece => piece.Restrictions.Any(restriction => restriction.IsConfining))
            .Select(piece => piece.Id),
    ];

    /// <summary>Whether a palace lets <paramref name="move"/> be made, in a game with palaces or without.</summary>
    public static bool MayMove([Required] GameState state, [Required] Move move) =>
        !Confined.Contains(move.Mover.Kind)
        || (state.Palaces ?? []).Values.FirstOrDefault(palace => palace.Contains(move.From)) is not { } held
        || held.Contains(move.To)
        || IsFlyingCapture(state, move);

    /// <summary>The Xiangqi General taking a leader: by its flying-general capture, if not by a step.</summary>
    private static bool IsFlyingCapture(GameState state, Move move) =>
        move.Mover.Kind == GrandLegionsPieces.XiangqiGeneral
        && state.Position.PieceAt(move.To) is { } taken
        && GrandLegionsRules.Leaders.Contains(taken.Kind);
}
