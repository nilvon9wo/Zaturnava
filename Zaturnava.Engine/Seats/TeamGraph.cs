using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Engine.Seats;

/// <summary>
/// Which seats are allied: a partition of the seats into teams, of any sizes, resolved when a game
/// is created. A seat is always its own ally.
/// </summary>
public sealed class TeamGraph(ImmutableDictionary<SeatId, int> teamOfSeat)
{
    private readonly ImmutableDictionary<SeatId, int> _teamOfSeat = teamOfSeat;

    public static TeamGraph FromTeams([Required] IEnumerable<IEnumerable<SeatId>> teams) =>
        new(teams.SelectMany(TeamMembership).ToImmutableDictionary());

    public int TeamOf(SeatId seat) => this._teamOfSeat[seat];

    public bool AreAllied(SeatId first, SeatId second) => this.TeamOf(first) == this.TeamOf(second);

    private static IEnumerable<KeyValuePair<SeatId, int>> TeamMembership(IEnumerable<SeatId> team, int index) =>
        team.Select(seat => KeyValuePair.Create(seat, index));
}
