using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class AlliancesTest
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(2, 1, 3)]
    [InlineData(3, 1, 2)]
    public void AlliedWith_WhenGivenSeatZerosPartner_AlliesTheOtherTwoWithEachOther(int partner, int other, int third)
    {
        // Act
        TeamGraph teams = Alliances.AlliedWith(partner);

        // Assert
        Assert.True(teams.AreAllied(new SeatId(0), new SeatId(partner)));
        Assert.True(teams.AreAllied(new SeatId(other), new SeatId(third)));
        Assert.False(teams.AreAllied(new SeatId(0), new SeatId(other)));
    }

    [Fact]
    public void EveryoneForThemselves_WhenAsked_AlliesNoTwoSeats()
    {
        // Act
        TeamGraph teams = Alliances.EveryoneForThemselves();

        // Assert
        Assert.Equal(4, Enumerable.Range(0, 4).Select(seat => teams.TeamOf(new SeatId(seat))).Distinct().Count());
    }
}
