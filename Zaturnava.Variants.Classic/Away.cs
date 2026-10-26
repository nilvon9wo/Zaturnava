using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// A player away from the game (rules.md §10): the others may vote to stop waiting for them, and their
/// turn then passes; once it has so passed some turns in a row, they have given up their turns, as if they
/// had chosen to. Whatever they play themselves forgets it all; saying they are back (rejoining) has them
/// waited for again, without forgetting how many turns they missed.
/// </summary>
internal static class Away
{
    public static bool MayVote(GameState state, SeatId voter, SeatId absent) =>
        state.Outcome is null
        && voter != absent
        && GameEnd.IsPlaying(state, voter)
        && !state.Resigned.Contains(voter)
        && GameEnd.IsPlaying(state, absent);

    public static Transition Voted(GameState state, VoteAwayAction vote)
    {
        Absence absence = state.Absence;
        AwayVote stop = new(vote.Actor, vote.Absent);
        Absence voted = absence with
        {
            StopVotes = vote.Stops ? absence.StopVotes.Add(stop) : absence.StopVotes.Remove(stop),
        };
        return new Transition(state with { Absence = voted }, [new AwayVoted(vote.Actor, vote.Absent, vote.Stops)]);
    }

    /// <summary>
    /// The absent seat missed one more turn, and is no longer counted back; at <paramref name="limit"/> in
    /// a row, it has given up its turns. The pass itself is the caller's.
    /// </summary>
    public static Transition Missed(GameState state, SeatId seat, int limit)
    {
        Absence absence = state.Absence;
        int missed = absence.MissedBy(seat) + 1;
        GameState counted = state with
        {
            Absence = absence with
            {
                Missed = absence.Missed.SetItem(seat, missed),
                Returning = absence.Returning.Remove(seat),
            },
            Resigned = missed >= limit ? state.Resigned.Add(seat) : state.Resigned,
        };
        return new Transition(counted, [new TurnPassedAway(seat, missed, limit)]);
    }

    /// <summary>Whether <paramref name="action"/> is its actor playing, not voting, rejoining or rolling.</summary>
    public static bool IsPlaying(GameAction action) =>
        action is not (VoteAwayAction or PassAwayAction or RejoinAction or DiceAction);

    /// <summary>The seat is playing: it is no longer away.</summary>
    public static Transition Present(Transition transition, SeatId seat) =>
        transition with
        {
            State = transition.State with { Absence = transition.State.Absence.Forgetting(seat) },
        };
}
