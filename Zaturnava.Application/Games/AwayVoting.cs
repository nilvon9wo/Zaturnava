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
public sealed record AwayVoting(
    SeatId Absent,
    ImmutableList<SeatId> Voters,
    ImmutableHashSet<SeatId> Stopping,
    TimeSpan GraceLeft
)
{
    public bool IsOpen => this.GraceLeft == TimeSpan.Zero;

    public bool IsAgreed => this.Voters.All(this.Stopping.Contains);
}
