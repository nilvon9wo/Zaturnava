using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Games;

/// <summary>
/// The people who may vote to stop waiting for <see cref="Absent"/>, a seat not played by a bot, deciding
/// what no timer limits: <see cref="Voters"/>, every other person still playing. The vote
/// <see cref="IsOpen"/> once the seat has had its grace (none, once its turns have passed while away, until
/// it says it is back); and its turn passes once every voter would stop waiting (<see cref="IsAgreed"/>),
/// <see cref="GraceLeft"/> from now.
/// </summary>
/// <remarks>
/// Once the vote is open, a voter may also have a bot take the seat for the rest of the game (Proposed): the first
/// person to have taken a seat at the game (<see cref="FirstArrival"/>, the one who set it up, most often) at once;
/// and, in case they have gone too, any voter, <see cref="UntilAnyoneReplaces"/> from now.
/// </remarks>
public sealed record AwayVoting(
    SeatId Absent,
    ImmutableList<SeatId> Voters,
    ImmutableHashSet<SeatId> Stopping,
    TimeSpan GraceLeft
)
{
    public bool IsOpen => this.GraceLeft == TimeSpan.Zero;

    public bool IsAgreed => this.Voters.All(this.Stopping.Contains);

    /// <summary>The first person to have taken a seat at the game; none, if no one has.</summary>
    public SeatId? FirstArrival { get; init; }

    /// <summary>How long until any voter may have a bot take the seat away.</summary>
    public TimeSpan UntilAnyoneReplaces { get; init; }

    /// <summary>Whether <paramref name="seat"/> may have a bot take the seat away now.</summary>
    public bool MayReplace(SeatId seat) =>
        this.IsOpen
        && this.Voters.Contains(seat)
        && (seat == this.FirstArrival || this.UntilAnyoneReplaces == TimeSpan.Zero);
}
