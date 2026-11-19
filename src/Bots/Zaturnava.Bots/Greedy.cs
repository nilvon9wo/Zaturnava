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
/// the team's best piece, and never into an enemy's, and a piece by its own promotion into the best it may become;
/// a promotion weighed by the mobility of what it makes too, and among equals the bot's own army's piece comes first.
/// An escape, likewise, gains what it puts back.
/// </summary>
internal static class Greedy
{
    /// <summary>
    /// How many squares a piece promoted into must reach on an open board for each point more it is weighed.
    /// </summary>
    private const int SquaresPerPoint = 4;

    public static GameAction Choose(DecisionRequest request, Random random)
    {
        ImmutableList<GameAction> choices = Choices(request);
        return choices[random.Next(choices.Count)];
    }

    /// <summary>The best scoring actions, if any score; otherwise every action that does something.</summary>
    internal static ImmutableList<GameAction> Choices(DecisionRequest request) => Weighed(request).Choices;

    /// <summary>
    /// The best scoring actions, if any score (<paramref name="request"/>'s <c>Scores</c>); otherwise every action
    /// that does something.
    /// </summary>
    internal static (ImmutableList<GameAction> Choices, bool Scores) Weighed(DecisionRequest request)
    {
        ImmutableList<(GameAction Action, int Gain)> gains =
            [.. Playable.Of(request).Select(action => (action, Gain(request, action, After(request, action))))];
        int best = gains.Max(gain => gain.Gain);
        return best > 0
            ? (Preferred(request, [.. gains.Where(gain => gain.Gain == best).Select(gain => gain.Action)]), true)
            : (Active(gains), false);
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

    /// <summary>What the action scores the bot, and what a promotion puts on the board for its team.</summary>
    internal static int Gain(DecisionRequest request, GameAction action, GameState after) =>
        ScoreOf(after, request.Seat)
            - ScoreOf(request.State, request.Seat)
            + action switch
            {
                PromoteAction promotion => PromotionGain(request, promotion),
                PromoteOwnAction promotion => OwnPromotionGain(request, promotion),
                EscapeAction escape => SideOf(request, PrisonerOf(request, escape.Piece)),
                _ => 0,
            };

    /// <summary>What the piece brought back is worth to the bot's team, less what the Pawn was.</summary>
    private static int PromotionGain(DecisionRequest request, PromoteAction promotion)
    {
        GameState state = request.State;
        Piece pawn = state.Position.PieceAt(promotion.Cell)!; // a legal promotion has a Pawn to promote
        return PromotedOf(request, PrisonerOf(request, promotion.Into)) - SideOf(request, pawn);
    }

    /// <summary>What the piece becomes is worth to the bot's team, less what it was.</summary>
    private static int OwnPromotionGain(DecisionRequest request, PromoteOwnAction promotion)
    {
        Piece piece = request.State.Position.PieceAt(promotion.Cell)!; // a legal promotion has a piece to promote
        return PromotedOf(request, piece with { Kind = promotion.Becomes }) - SideOf(request, piece);
    }

    /// <summary>
    /// What a piece a promotion puts on the board is worth to the bot's team, to choose among promotions only: its
    /// worth, and a point for each <see cref="SquaresPerPoint"/> squares it reaches on an open board, its mobility
    /// (the user's word of 2026-11-19, on the playtest of 2026-11-15, F2: "selection should consider strategy - such
    /// as piece mobility and player coordination, not just points", where a Pawn became a Raja, worth 5, not the Rook,
    /// worth 4, that would have won; the Raja steps to 8 squares, the Rook slides to 16). Which army it joins, the
    /// coordination, settles the rest (<see cref="Preferred"/>). The piece's own worth is unchanged.
    /// </summary>
    private static int PromotedOf(DecisionRequest request, Piece piece)
    {
        int side = SideOf(request, piece);
        return side + (Math.Sign(side) * (request.Rules.GuideOf(piece.Kind).Diagram.Moves.Count / SquaresPerPoint));
    }

    /// <summary>
    /// Of the actions that gain as much, a promotion into a piece of the bot's own army first (the playtest of
    /// 2026-11-15, F2): it moves on the bot's own turns, as an ally's does not.
    /// </summary>
    private static ImmutableList<GameAction> Preferred(DecisionRequest request, ImmutableList<GameAction> equals)
    {
        ImmutableList<GameAction> own = [.. equals.Where(action => IsIntoOwn(request, action))];
        return own.IsEmpty ? equals : own;
    }

    private static bool IsIntoOwn(DecisionRequest request, GameAction action) =>
        action is PromoteAction promotion && PrisonerOf(request, promotion.Into).Owner == request.Seat;

    /// <summary>The captured piece; or one that fled the board, which comes back as a prisoner escapes.</summary>
    private static Piece PrisonerOf(DecisionRequest request, PieceId piece) =>
        request.State.Prisoners.Select(prisoner => prisoner.Piece)
            .Concat(request.State.Fled)
            .First(held => held.Id == piece);

    /// <summary>A piece's worth, for the bot's team; less than nothing, for an enemy's.</summary>
    private static int SideOf(DecisionRequest request, Piece piece) =>
        request.State.Teams.AreAllied(piece.Owner, request.Seat)
            ? request.Rules.WorthOf(piece.Kind)
            : -request.Rules.WorthOf(piece.Kind);

    private static GameState After(DecisionRequest request, GameAction action) =>
        request.Rules.ApplyLegal(request.State, action).State;

    private static int ScoreOf(GameState state, SeatId seat) => state.Scores.GetValueOrDefault(seat);
}
