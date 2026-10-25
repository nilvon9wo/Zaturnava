using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// Play on the board that looks no further than the next action: whichever legal action gains the
/// most at once, at random among equals; when nothing gains, any action that does something and loses
/// nothing, passing only when nothing else is left. An action gains what it scores, and a promotion
/// what it puts on the board for the bot's team, less what it takes off: so a Pawn is promoted into
/// the team's best piece, and never into an enemy's.
/// </summary>
internal static class Greedy
{
    public static GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<GameAction> choices = Choices(request);
        return choices[random.Next(choices.Count)];
    }

    /// <summary>The best scoring actions, if any score; otherwise every action that does something.</summary>
    private static ImmutableList<GameAction> Choices(DecisionRequest request)
    {
        ImmutableList<(GameAction Action, int Gain)> gains =
            [.. request.LegalActions.Select(action => (action, Gain(request, action)))];
        int best = gains.Max(gain => gain.Gain);
        return best > 0
            ? [.. gains.Where(gain => gain.Gain == best).Select(gain => gain.Action)]
            : Active(gains);
    }

    /// <summary>Anything but passing that loses nothing, if there is anything such; else passing.</summary>
    private static ImmutableList<GameAction> Active(ImmutableList<(GameAction Action, int Gain)> gains)
    {
        ImmutableList<GameAction> active =
            [.. gains.Where(gain => gain.Action is not PassAction && gain.Gain == 0).Select(gain => gain.Action)];
        return active.IsEmpty
            ? [.. gains.Where(gain => gain.Action is PassAction).Select(gain => gain.Action)]
            : active;
    }

    private static int Gain(DecisionRequest request, GameAction action) =>
        ScoreOf(request.Rules.Apply(request.State, action).State, request.Seat)
            - ScoreOf(request.State, request.Seat)
            + (action is PromoteAction promotion ? PromotionGain(request, promotion) : 0);

    /// <summary>What the piece brought back is worth to the bot's team, less what the Pawn was.</summary>
    private static int PromotionGain(DecisionRequest request, PromoteAction promotion)
    {
        GameState state = request.State;
        Piece pawn = state.Position.PieceAt(promotion.Cell)!; // a legal promotion has a Pawn to promote
        Piece into = state.Prisoners.First(prisoner => prisoner.Piece.Id == promotion.Into).Piece;
        return SideOf(request, into) - SideOf(request, pawn);
    }

    /// <summary>A piece's worth, for the bot's team; less than nothing, for an enemy's.</summary>
    private static int SideOf(DecisionRequest request, Piece piece) =>
        request.State.Teams.AreAllied(piece.Owner, request.Seat)
            ? request.Rules.WorthOf(piece.Kind)
            : -request.Rules.WorthOf(piece.Kind);

    private static int ScoreOf(GameState state, SeatId seat) => state.Scores.GetValueOrDefault(seat);
}
