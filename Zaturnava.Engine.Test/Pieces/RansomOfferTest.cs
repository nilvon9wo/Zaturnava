using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Pieces;

public sealed class RansomOfferTest
{
    private static readonly RansomOffer Offer = new(new SeatId(0), new SeatId(1), [new PieceId(8)], [new PieceId(3)]);

    public static TheoryData<RansomOffer> OtherOffers =>
    [
        Offer with { To = new SeatId(3) },
        Offer with { Given = [new PieceId(9)] },
        Offer with { Asked = [] },
    ];

    [Fact]
    public void Equals_WhenTheSameOffer_IsTrue()
    {
        // Arrange
        RansomOffer same = new(new SeatId(0), new SeatId(1), [new PieceId(8)], [new PieceId(3)]);

        // Act
        bool isEqual = Offer.Equals(same);

        // Assert
        Assert.Equal((true, Offer.GetHashCode()), (isEqual, same.GetHashCode()));
    }

    [Fact]
    public void Equals_WhenComparedWithNothing_IsFalse()
    {
        // Arrange - nothing beyond the offer

        // Act
        bool isEqual = Offer.Equals(null);

        // Assert
        Assert.False(isEqual);
    }

    [Theory]
    [MemberData(nameof(OtherOffers), DisableDiscoveryEnumeration = true)]
    public void Equals_WhenAnythingDiffers_IsFalse(RansomOffer other)
    {
        // Arrange - nothing beyond the offers

        // Act
        bool isEqual = Offer.Equals(other);

        // Assert
        Assert.False(isEqual);
    }
}
