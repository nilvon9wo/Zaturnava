using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How a bot ends a game. Asked to end it, it agrees if its team leads on points, or if its game is
/// hopeless: behind by a <see cref="ClearLead"/> on points and on the board both, with no leader and no
/// major piece left to its team while its enemies have one. Otherwise, when an ally has given up its turns
/// with that clear lead, and no one else in the game is a person, it gives up its own as well, ending the
/// game.
/// </summary>
internal static class Ending
{
    /// <summary>How far ahead, on points and on the board both, a lead is clear (rules.md §10).</summary>
    public const int ClearLead = 10;

    /// <summary>What a major piece is worth at least: a Rook's worth, as the Chaturaji 2027 Elephant.</summary>
    public const int MajorWorth = 4;

    /// <summary>
    /// The answer to the proposal waiting, if one is; else giving up its turns beside a resting ally, if it
    /// would; else null.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request) =>
        request.State.Proposal is null
            ? RestingWith(request)
            : new AnswerEndAction(request.Seat, IsAhead(request) || IsHopeless(request));

    private static ProposeEndAction? RestingWith(DecisionRequest request)
    {
        GameState state = request.State;
        SeatId seat = request.Seat;
        bool hasRestingAlly = state.Resigned.Any(other => other != seat && state.Teams.AreAllied(other, seat));
        bool isAmongBots = request.People.All(person =>
            state.Resigned.Contains(person) && state.Teams.AreAllied(person, seat)
        );
        bool isClear = PointsLead(request) >= ClearLead && BoardLead(request) >= ClearLead;
        return hasRestingAlly && isAmongBots && isClear && request.Rules.MayProposeEnd(state, seat)
            ? new ProposeEndAction(seat, Resigns: true)
            : null;
    }

    private static bool IsAhead(DecisionRequest request) => PointsLead(request) > 0;

    private static bool IsHopeless(DecisionRequest request)
    {
        ILookup<bool, Piece> majors = OnBoard(request)
            .Where(piece => request.Rules.IsLeader(piece.Kind) || request.Rules.WorthOf(piece.Kind) >= MajorWorth)
            .ToLookup(piece => request.State.Teams.AreAllied(piece.Owner, request.Seat));
        return PointsLead(request) <= -ClearLead
            && BoardLead(request) <= -ClearLead
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
