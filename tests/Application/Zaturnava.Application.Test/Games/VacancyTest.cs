using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class VacancyTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Later = new(2);
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    [Theory]
    [InlineData(0, 60, 120, false)]
    [InlineData(0, 0, 60, true)]
    [InlineData(2, 0, 60, false)]
    [InlineData(2, 0, 0, true)]
    public void MayFill_WhenAsked_LetsTheFirstArrivalAfterTheWaitAndAnyoneAfterTwice(
        int seat,
        int untilFirst,
        int untilAnyone,
        bool expected
    )
    {
        // Arrange
        Vacancy vacancy = new(First, TimeSpan.FromSeconds(untilFirst), TimeSpan.FromSeconds(untilAnyone));

        // Act
        bool mayFill = vacancy.MayFill(new SeatId(seat));

        // Assert
        Assert.Equal(expected, mayFill);
    }

    [Fact]
    public void MayFill_WhenNoOneArrivedFirst_LetsNoOneBeforeTwiceTheWait()
    {
        // Arrange
        Vacancy vacancy = new(null, TimeSpan.Zero, Minute);

        // Act
        bool mayFill = vacancy.MayFill(Later);

        // Assert
        Assert.False(mayFill);
    }
}
