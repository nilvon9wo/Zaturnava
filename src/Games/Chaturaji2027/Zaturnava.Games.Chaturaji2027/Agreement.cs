using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Ending the game by agreement (rules.md §10): any seat still playing may propose it, whoever's turn it
/// is, giving up its own turns meanwhile if it likes. The game waits while each other seat still playing
/// answers, clockwise from the proposer - but for any that has given up its turns, which wants it over
/// already. Once all have agreed, the game ends, and points decide; once one refuses, it goes on where
/// it was. A seat that gave up its turns may take them back, once no proposal waits; so may one that
/// missed turns while away say it is back.
/// </summary>
internal static class Agreement
{
    public const string ByAgreement = "every player agreed to end it, so points decide";

    public static bool MayPropose(GameState state, SeatId seat) =>
        state.Outcome is null
        && state.Proposal is null
        && state.AwaitedDice is null
        && state.RollOff.IsEmpty
        && GameEnd.IsPlaying(state, seat)
        && !state.Resigned.Contains(seat);

    /// <summary>
    /// A seat may rejoin once it has given up its turns, or, if it has missed turns while away, to be
    /// waited for again; not while a proposal waits.
    /// </summary>
    public static bool MayRejoin(GameState state, SeatId seat) =>
        state.Outcome is null
        && state.Proposal is null
        && (state.Resigned.Contains(seat)
            || (state.Absence.MissedBy(seat) > 0 && !state.Absence.Returning.Contains(seat)));

    public static Transition Rejoined(GameState state, RejoinAction rejoin)
    {
        SeatId seat = rejoin.Actor;
        Absence absence = state.Absence;
        GameState rejoined = state with
        {
            Resigned = state.Resigned.Remove(seat),
            Absence = absence.MissedBy(seat) > 0 ? absence with { Returning = absence.Returning.Add(seat) } : absence,
        };
        return new Transition(rejoined, [new Rejoined(seat)]);
    }

    public static Transition Proposed(GameState state, ProposeEndAction propose)
    {
        SeatId proposer = propose.Actor;
        GameState proposed = state with
        {
            // a game not over has a seat deciding
            Proposal = new EndProposal(proposer, propose.Resigns, state.DecidingSeat!.Value, state.ActingFor),
            Resigned = propose.Resigns ? state.Resigned.Add(proposer) : state.Resigned,
        };
        bool isAhead = GameEnd.PointsLead(state, state.Teams.TeamOf(proposer)) > 0;
        return AskedAfter(
            new Transition(proposed, [new EndProposed(proposer, propose.Resigns, isAhead)]),
            proposer
        );
    }

    public static Transition Answered(GameState state, AnswerEndAction answer) =>
        answer.Agrees
            ? AskedAfter(new Transition(state, [new EndAgreed(answer.Actor)]), answer.Actor)
            : Refused(state, answer.Actor);

    /// <summary>The next seat clockwise to answer, before coming round to the proposer; if none is, the end.</summary>
    private static Transition AskedAfter(Transition transition, SeatId seat)
    {
        GameState state = transition.State;
        SeatId proposer = state.Proposal!.Proposer; // asked only while a proposal waits
        SeatId? next = Enumerable
            .Range(1, state.Seats.Count)
            .Select(offset => new SeatId((seat.Value + offset) % state.Seats.Count))
            .TakeWhile(other => other != proposer)
            .Where(other => GameEnd.IsPlaying(state, other) && !state.Resigned.Contains(other))
            .Cast<SeatId?>()
            .FirstOrDefault();
        return next is { } voter
            ? transition with { State = state with { DecidingSeat = voter, ActingFor = null } }
            : GameEnd.ByPoints(
                transition with { State = state with { Proposal = null } },
                Chaturaji2027Endings.Agreement,
                ByAgreement
            );
    }

    private static Transition Refused(GameState state, SeatId seat)
    {
        EndProposal proposal = state.Proposal!; // only an answer refuses, while a proposal waits
        GameState resumed = state with
        {
            Proposal = null,
            DecidingSeat = proposal.Resume,
            ActingFor = proposal.ResumeActingFor,
        };
        return new Transition(resumed, [new EndRefused(seat)]);
    }
}
