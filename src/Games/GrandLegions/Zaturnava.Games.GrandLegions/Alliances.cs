using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// Who is allied with whom (specification §2): the creator picks any two of the four seats as allies, and the
/// other two are allied by implication; or there are no teams.
/// </summary>
public static class Alliances
{
    private const int SeatCount = 4;

    /// <summary>Seat 0 allied with <paramref name="partner"/>, and the other two seats with each other.</summary>
    public static TeamGraph AlliedWith([Range(1, SeatCount - 1)] int partner)
    {
        ImmutableList<SeatId> ours = [new(0), new(partner)];
        ImmutableList<SeatId> theirs =
            [.. Enumerable.Range(1, SeatCount - 1).Where(seat => seat != partner).Select(Seat)];
        return TeamGraph.FromTeams([ours, theirs]);
    }

    /// <summary>No teams: everyone for themselves.</summary>
    public static TeamGraph EveryoneForThemselves() =>
        TeamGraph.FromTeams(Enumerable.Range(0, SeatCount).Select(seat => ImmutableList.Create(Seat(seat))));

    private static SeatId Seat(int seat) => new(seat);
}
