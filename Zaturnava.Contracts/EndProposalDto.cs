using System.Collections.Immutable;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A proposal to end the game, waiting on the deciding seat's answer: seat <see cref="Proposer"/> made
/// it, and, if <see cref="Resigns"/>, gave up its turns meanwhile. A seat proposes whoever's turn it is,
/// so the proposal is not among a game's legal actions: a client makes one with <see cref="ActionOf"/>
/// when the seat may (<see cref="SeatDto.MayProposeEnd"/>).
/// </summary>
public sealed record EndProposalDto(int Proposer, bool Resigns)
{
    public const string Kind = "propose-end";
    public const string ResignsArgument = "resigns";

    /// <summary>The action by which <paramref name="actor"/> proposes ending the game.</summary>
    public static ActionDto ActionOf(int actor, bool resigns) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, actor.ToString(CultureInfo.InvariantCulture))
                .Add(ResignsArgument, resigns ? "true" : "false")
        );
}
