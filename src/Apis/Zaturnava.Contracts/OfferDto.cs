using System.Collections.Immutable;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A ransom waiting on its answer: seat <see cref="From"/> offers seat <see cref="To"/> back the
/// prisoners <see cref="Given"/>, for the prisoners <see cref="Asked"/>; none asked makes it a gift.
/// Offers are not among a game's legal actions - there are too many - so a client makes one with
/// <see cref="ActionOf"/> when the game says it may.
/// </summary>
public sealed record OfferDto(int From, int To, ImmutableList<PrisonerDto> Given, ImmutableList<PrisonerDto> Asked)
{
    public const string Kind = "offer";
    public const string ReceiverArgument = "receiver";
    public const string GivenArgument = "given";
    public const string AskedArgument = "asked";

    /// <summary>The action by which <paramref name="actor"/> offers the prisoners, by piece id.</summary>
    public static ActionDto ActionOf(int actor, int receiver, IEnumerable<int> given, IEnumerable<int> asked) =>
        new(
            Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, Text(actor))
                .Add(ReceiverArgument, Text(receiver))
                .Add(GivenArgument, string.Join(',', given.Select(Text)))
                .Add(AskedArgument, string.Join(',', asked.Select(Text)))
        );

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
