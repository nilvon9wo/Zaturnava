using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Actions;

/// <summary>
/// Offer <see cref="Receiver"/> back the prisoners <see cref="Given"/>, for the prisoners
/// <see cref="Asked"/> in return; asking nothing makes it a gift. The receiver accepts or declines.
/// </summary>
public sealed record OfferAction(
    SeatId Actor,
    SeatId Receiver,
    ImmutableList<PieceId> Given,
    ImmutableList<PieceId> Asked
) : GameAction(Actor)
{
    public const string Kind = "offer";
    public const string ReceiverArgument = "receiver";
    public const string GivenArgument = "given";
    public const string AskedArgument = "asked";

    public RansomOffer Offer => new(this.Actor, this.Receiver, this.Given, this.Asked);

    public override ActionDescriptor Describe() =>
        new(
            Kind,
            this.ActorArguments()
                .Add(ReceiverArgument, Text(this.Receiver.Value))
                .Add(GivenArgument, Listed(this.Given))
                .Add(AskedArgument, Listed(this.Asked))
        );

    /// <exception cref="MalformedActionException">An argument is missing or not integers.</exception>
    public static OfferAction Parse([Required] ActionDescriptor descriptor)
    {
        SeatId actor = descriptor.Actor();
        SeatId receiver = new(descriptor.IntegerArgument(ReceiverArgument));
        ImmutableList<PieceId> given = [.. descriptor.IntegersArgument(GivenArgument).Select(id => new PieceId(id))];
        ImmutableList<PieceId> asked = [.. descriptor.IntegersArgument(AskedArgument).Select(id => new PieceId(id))];
        return new OfferAction(actor, receiver, given, asked);
    }

    public bool Equals(OfferAction? other) => other is not null && this.Offer == other.Offer;

    public override int GetHashCode() => this.Offer.GetHashCode();

    private static string Listed(ImmutableList<PieceId> pieces) =>
        string.Join(ActionDescriptor.ListSeparator, pieces.Select(piece => Text(piece.Value)));
}
