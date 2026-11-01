using System.Collections.Immutable;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The vote on waiting for <see cref="Seat"/>, deciding what no timer limits: the people who may vote
/// (<see cref="Voters"/>), and those of them who would stop waiting (<see cref="Stopping"/>). Once the
/// vote <see cref="IsOpen"/>, a voter may say so with <see cref="ActionOf"/>; and once every voter would,
/// the seat's turn passes when its <see cref="GameViewDto.SecondsLeft"/> run out. A vote to keep waiting
/// may be cast at any time. Once the vote is open, a voter may also have a bot take the seat for the rest of the game
/// (<see cref="GameRoutes.Replace"/>): the first person to have taken a seat (<see cref="FirstArrival"/>) at once,
/// and any voter once <see cref="SecondsUntilAnyoneReplaces"/> run out.
/// </summary>
public sealed record AwayDto(
    int Seat,
    bool IsOpen,
    ImmutableList<int> Voters,
    ImmutableList<int> Stopping,
    int? FirstArrival = null,
    double SecondsUntilAnyoneReplaces = 0
)
{
    /// <summary>Whether <paramref name="seat"/> may have a bot take the seat away now.</summary>
    public bool MayReplace(int seat) =>
        this.IsOpen
        && this.Voters.Contains(seat)
        && (seat == this.FirstArrival || this.SecondsUntilAnyoneReplaces <= 0);

    public const string Kind = "vote-away";
    public const string AbsentArgument = "absent";
    public const string StopsArgument = "stops";

    /// <summary>
    /// The action by which <paramref name="voter"/> would stop waiting for <paramref name="absent"/>, or wait.
    /// </summary>
    public static ActionDto ActionOf(int voter, int absent, bool stops) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, voter.ToString(CultureInfo.InvariantCulture))
                .Add(AbsentArgument, absent.ToString(CultureInfo.InvariantCulture))
                .Add(StopsArgument, stops ? "true" : "false")
        );
}
