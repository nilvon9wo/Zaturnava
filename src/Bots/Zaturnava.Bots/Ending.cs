using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How a bot ends a game, a lead clear by the ruleset's <see cref="IRulesApplying.ClearLead"/>. Asked to end it, it
/// agrees if its team leads on points, if its game is hopeless - behind by a clear lead on points and on the board
/// both, with no leader and no heavy piece left to its team while its enemies have one - or if an ally has given up
/// its turns while the team trails by a clear lead on points (Proposed). Otherwise, when an ally has given up its
/// turns and no one else in the game is a person, it gives up its own as well, ending the game, if its team leads
/// clearly, or trails clearly (Proposed): an ally that gave up concedes a game it trails, so playing on alone only
/// keeps everyone waiting for a game already lost.
/// </summary>
internal static class Ending
{
    /// <summary>
    /// The answer to the proposal waiting, if one is; else giving up its turns beside a resting ally, if it
    /// would; else null.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request) =>
        request.State.Proposal is null
            ? RestingWith(request)
            : new AnswerEndAction(request.Seat, IsAhead(request) || IsHopeless(request) || IsConceded(request));

    private static ProposeEndAction? RestingWith(DecisionRequest request)
    {
        GameState state = request.State;
        SeatId seat = request.Seat;
        bool isAmongBots = request.People.All(person =>
            state.Resigned.Contains(person) && state.Teams.AreAllied(person, seat)
        );
        int clear = request.Rules.ClearLead;
        bool isClear = PointsLead(request) >= clear && BoardLead(request) >= clear;
        return HasRestingAlly(request) && isAmongBots && (isClear || IsConceded(request))
            && request.Rules.MayProposeEnd(state, seat)
            ? new ProposeEndAction(seat, Resigns: true)
            : null;
    }

    private static bool IsAhead(DecisionRequest request) => PointsLead(request) > 0;

    /// <summary>Whether an ally has given up its turns while the team trails by a clear lead on points.</summary>
    private static bool IsConceded(DecisionRequest request) =>
        HasRestingAlly(request) && PointsLead(request) <= -request.Rules.ClearLead;

    private static bool HasRestingAlly(DecisionRequest request) =>
        request.State.Resigned.Any(other =>
            other != request.Seat && request.State.Teams.AreAllied(other, request.Seat)
        );

    private static bool IsHopeless(DecisionRequest request)
    {
        ILookup<bool, Piece> majors = OnBoard(request)
            .Where(piece => request.Rules.IsLeader(piece.Kind) || request.Rules.IsHeavy(piece.Kind))
            .ToLookup(piece => request.State.Teams.AreAllied(piece.Owner, request.Seat));
        int clear = request.Rules.ClearLead;
        return PointsLead(request) <= -clear
            && BoardLead(request) <= -clear
            && !majors[true].Any()
            && majors[false].Any();
    }

    /// <summary>The points of the bot's team, less its enemies'.</summary>
    private static int PointsLead(DecisionRequest request) =>
        request.State.Scores.Sum(score => Side(request, score.Key) * score.Value);

    /// <summary>What the pieces of the bot's team on the board would score if taken, less its enemies'.</summary>
    private static int BoardLead(DecisionRequest request) =>
        OnBoard(request).Sum(piece => Side(request, piece.Owner) * request.Rules.WorthOf(piece.Kind));

    /// <summary>The pieces on the board, but a retired seat's.</summary>
    private static ImmutableList<Piece> OnBoard(DecisionRequest request) =>
        [.. request.State.Position.Occupants.Values.Where(piece => !request.State.Retired.Contains(piece.Owner))];

    private static int Side(DecisionRequest request, SeatId seat) =>
        request.State.Teams.AreAllied(seat, request.Seat) ? 1 : -1;
}
