using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// Who is away from the game: how many turns in a row each seat has had passed while it was away
/// (<see cref="Missed"/>); each seat's standing vote to stop waiting for another (<see cref="StopVotes"/>);
/// and the seats that have said they are back since (<see cref="Returning"/>), which are waited for again.
/// </summary>
public sealed record Absence(
    ImmutableDictionary<SeatId, int> Missed,
    ImmutableHashSet<AwayVote> StopVotes,
    ImmutableHashSet<SeatId> Returning
)
{
    public static readonly Absence None = new([], [], []);

    public int MissedBy(SeatId seat) => this.Missed.GetValueOrDefault(seat);

    /// <summary>The seats that would stop waiting for <paramref name="absent"/>.</summary>
    public ImmutableHashSet<SeatId> StoppingFor(SeatId absent) =>
        [.. this.StopVotes.Where(vote => vote.Absent == absent).Select(vote => vote.Voter)];

    /// <summary>Everything about <paramref name="seat"/>'s absence forgotten: it is playing again.</summary>
    public Absence Forgetting(SeatId seat) =>
        new(
            this.Missed.Remove(seat),
            [.. this.StopVotes.Where(vote => vote.Absent != seat)],
            this.Returning.Remove(seat)
        );
}

/// <summary><see cref="Voter"/> would stop waiting for <see cref="Absent"/>.</summary>
public sealed record AwayVote(SeatId Voter, SeatId Absent);
