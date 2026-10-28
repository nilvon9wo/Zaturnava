using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class OfferActionTest
{
    private static readonly SeatId Offerer = new(0);
    private static readonly SeatId Receiver = new(1);

    [Fact]
    public void Describe_WhenCalled_ListsThePiecesGivenAndAsked()
    {
        // Arrange
        OfferAction offer = new(Offerer, Receiver, [new PieceId(8), new PieceId(9)], []);

        // Act
        ActionDescriptor descriptor = offer.Describe();

        // Assert
        Assert.Equal(
            ("offer", "1", "8,9", string.Empty),
            (
                descriptor.Kind,
                descriptor.Arguments["receiver"],
                descriptor.Arguments["given"],
                descriptor.Arguments["asked"]
            )
        );
    }

    [Fact]
    public void Offer_WhenRead_IsTheRansomOffered()
    {
        // Arrange
        OfferAction offer = new(Offerer, Receiver, [new PieceId(8)], [new PieceId(3)]);

        // Act
        RansomOffer ransom = offer.Offer;

        // Assert
        Assert.Equal(new RansomOffer(Offerer, Receiver, [new PieceId(8)], [new PieceId(3)]), ransom);
    }

    [Fact]
    public void Equals_WhenTheSameOffer_IsTrue()
    {
        // Arrange
        OfferAction offer = new(Offerer, Receiver, [new PieceId(8)], [new PieceId(3)]);
        OfferAction same = new(Offerer, Receiver, [new PieceId(8)], [new PieceId(3)]);

        // Act
        bool isEqual = offer.Equals(same);

        // Assert
        Assert.Equal((true, offer.GetHashCode()), (isEqual, same.GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange
        OfferAction offer = new(Offerer, Receiver, [new PieceId(8)], []);

        // Act
        bool isEqual = offer.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Fact]
    public void Parse_WhenAPieceIsNotAnInteger_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new(
            OfferAction.Kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add("actor", "0")
                .Add("receiver", "1")
                .Add("given", "8,eight")
        );

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => OfferAction.Parse(descriptor)
        );

        // Assert
        Assert.Equal("The 'offer' action needs integers in 'given', such as 3,5.", exception.Message);
    }
}
