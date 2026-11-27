using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// How a bot ends a game, a lead clear by the ruleset's <see cref="IRulesApplying.ClearLead"/>. Asked to end it, it
/// agrees if its team leads on points, if its game is hopeless - behind by more than it could hope to take back
/// (<see cref="IsHopeless"/>) - or if an ally has given up its turns while the team trails by a clear lead on points
/// (Proposed); but never to a defeat while its own army is mostly whole (<see cref="IsArmyWhole"/>). Otherwise, when an
/// ally has given up its
/// turns and no one else in the game is a person, it gives up its own as well, ending the game, if its team leads
/// clearly, or trails clearly (Proposed): an ally that gave up concedes a game it trails, so playing on alone only
/// keeps everyone waiting for a game already lost. Failing that, it proposes to end a game already decided, won or
/// lost, playing on if anyone refuses (Asked 2026-11-25; its thresholds Proposed).
/// </summary>
internal static class Ending
{
    /// <summary>How a team's hope is counted against its face: erring to hope, but not by much (Proposed).</summary>
    public const double Optimism = 1.25;

    /// <summary>
    /// The answer to the proposal waiting, if one is; else giving up its turns beside a resting ally, if it
    /// would; else null; each by the bot's own <paramref name="temper"/>, else <see cref="Temper.Even"/>.
    /// </summary>
    public static GameAction? AnswerTo(DecisionRequest request, Temper? temper = null)
    {
        Temper own = temper ?? Temper.Even;
        return request.State.Proposal is null
            ? RestingWith(request, own) ?? Decided(request, own)
            : new AnswerEndAction(
                request.Seat,
                IsAhead(request) || (!IsArmyWhole(request) && (IsHopeless(request, own) || IsConceded(request)))
            );
    }

    /// <summary>
    /// How much of what its army was worth a bot must keep for it to call its army mostly whole, and refuse defeat
    /// (Proposed).
    /// </summary>
    public const double Whole = 0.75;

    /// <summary>
    /// Whether the bot's own army is mostly whole: what it has on the board is more than <see cref="Whole"/> of that
    /// and what enemies hold prisoner of it. Such a bot neither accepts defeat nor proposes it, however its team trails
    /// (the user, 2026-11-27, of a Grand Legions game: "the red army still has literally all their pieces ... red
    /// really shouldn't be so willing to concede while it still has its entire army!").
    /// </summary>
    private static bool IsArmyWhole(DecisionRequest request)
    {
        GameState state = request.State;
        int left = state.Position.Occupants.Values
            .Where(piece => piece.Owner == request.Seat)
            .Sum(piece => request.Rules.WorthOf(piece.Kind));
        int lost = state.Prisoners
            .Where(prisoner => prisoner.Piece.Owner == request.Seat
                && !state.Teams.AreAllied(prisoner.Holder, request.Seat)
            )
            .Sum(prisoner => request.Rules.WorthOf(prisoner.Piece.Kind));
        // strictly more: an army with nothing left, and nothing lost, is not whole
        return left > Whole * (left + lost);
    }

    /// <summary>
    /// How many turns a bot waits, once its proposal was refused, before it proposes again (Proposed): three rounds
    /// of a four-seat game.
    /// </summary>
    public const int Patience = 12;

    private static ProposeEndAction? RestingWith(DecisionRequest request, Temper temper)
    {
        GameState state = request.State;
        SeatId seat = request.Seat;
        bool isAmongBots = request.People.All(person =>
            state.Resigned.Contains(person) && state.Teams.AreAllied(person, seat)
        );
        return HasRestingAlly(request) && isAmongBots
            && (IsClearlyAhead(request, temper) || (IsConceded(request) && !IsArmyWhole(request)))
            && request.Rules.MayProposeEnd(state, seat)
            ? new ProposeEndAction(seat, Resigns: true)
            : null;
    }

    /// <summary>
    /// A proposal to end a game already decided - the team clearly ahead, on points and on the board, while the enemy
    /// has no hope left (<see cref="HasHope"/>), or hopeless itself, with no such hope of its own - playing on
    /// meanwhile, so that a refusal
    /// costs nothing; but not until <see cref="Patience"/> turns after the last of its team's was refused (the user's
    /// word of 2026-11-26: bots "on the same team will wait when proposals made by their teammate have recently been
    /// rejected").
    /// </summary>
    private static ProposeEndAction? Decided(DecisionRequest request, Temper temper)
    {
        GameState state = request.State;
        SeatId seat = request.Seat;
        bool isPatient = state.RefusedOn
            .Where(refusal => refusal.Key == seat || state.Teams.AreAllied(refusal.Key, seat))
            .All(refusal => state.TurnNumber - refusal.Value >= Patience);
        return isPatient && request.Rules.MayProposeEnd(state, seat)
            && ((IsClearlyAhead(request, temper) && !HasHope(request, Enemies))
                || (IsHopeless(request, temper) && !HasHope(request, Ours) && !IsArmyWhole(request)))
            ? new ProposeEndAction(seat, Resigns: false)
            : null;
    }

    private const int Ours = 1;
    private const int Enemies = -1;

    /// <summary>How near its promotion an enemy Pawn still gives its team hope, in steps (Stated 2026-11-26).</summary>
    public const int PromotionInReach = 2;

    /// <summary>
    /// Whether a team - the bot's own (<see cref="Ours"/>) or the enemy (<see cref="Enemies"/>) - but its retired
    /// seats, still has hope, whatever the lead: while it has a leader, or a heavy piece (as a Rook is), or a Pawn
    /// within <see cref="PromotionInReach"/> steps of its promotion (the user's word of 2026-11-26: "a team still has
    /// hope while it has at least one raja or one rook between them or a pawn just a step or two from promotion"). The
    /// bot's own team, so counted, proposes no defeat either (the user's games of 2026-11-27: Argus and Sage gave up
    /// with a Raja and a Rook left).
    /// </summary>
    private static bool HasHope(DecisionRequest request, int side)
    {
        GameState state = request.State;
        IRulesApplying rules = request.Rules;
        return state.Position.Occupants.Any(occupant =>
            Side(request, occupant.Value.Owner) == side
            && !state.Retired.Contains(occupant.Value.Owner)
            && (rules.IsLeader(occupant.Value.Kind)
                || rules.IsHeavy(occupant.Value.Kind)
                || rules.StepsToPromotion(state, occupant.Key) <= PromotionInReach)
        );
    }

    /// <summary>
    /// Whether the team leads both on points and on the board by a clear lead, as the bot's
    /// <paramref name="temper"/> counts one.
    /// </summary>
    private static bool IsClearlyAhead(DecisionRequest request, Temper temper)
    {
        double clear = temper.Lead * request.Rules.ClearLead;
        return PointsLead(request) >= clear && BoardLead(request) >= clear;
    }

    private static bool IsAhead(DecisionRequest request) => PointsLead(request) > 0;

    /// <summary>Whether an ally has given up its turns while the team trails by a clear lead on points.</summary>
    private static bool IsConceded(DecisionRequest request) =>
        HasRestingAlly(request) && PointsLead(request) <= -request.Rules.ClearLead;

    private static bool HasRestingAlly(DecisionRequest request) =>
        request.State.Resigned.Any(other =>
            other != request.Seat && request.State.Teams.AreAllied(other, request.Seat)
        );

    /// <summary>
    /// Whether the team trails by more than it could hope to take back (<see cref="Hope"/>), that hope counted at the
    /// bot's <see cref="Temper.Optimism"/> times its face, to err on the side of hope (rules.md §10, Stated
    /// 2026-11-06).
    /// </summary>
    private static bool IsHopeless(DecisionRequest request, Temper temper) =>
        -PointsLead(request) > temper.Optimism * Hope.Of(request);

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
