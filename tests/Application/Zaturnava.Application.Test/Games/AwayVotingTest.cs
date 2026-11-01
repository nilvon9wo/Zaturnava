using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class AwayVotingTest
{
    private static readonly SeatId Away = new(0);
    private static readonly SeatId First = new(1);
    private static readonly SeatId Other = new(2);

    [Theory]
    [InlineData(1, true, 0, 0, true)]
    [InlineData(1, true, 1, 0, false)]
    [InlineData(0, true, 0, 0, false)]
    [InlineData(2, true, 0, 1, false)]
    [InlineData(1, false, 0, 1, false)]
    [InlineData(2, true, 0, 0, true)]
    public void MayReplace_WhenAsked_AllowsAVoterOnceOpenFirstOrAfterTheWait(
        int asking,
        bool hasFirstArrival,
        int graceSeconds,
        int waitSeconds,
        bool mayReplace
    )
    {
        // Arrange
        AwayVoting voting = new(
            Away,
            [First, Other],
            [],
            TimeSpan.FromSeconds(graceSeconds)
        )
        {
            FirstArrival = hasFirstArrival ? First : null,
            UntilAnyoneReplaces = TimeSpan.FromSeconds(waitSeconds),
        };

        // Act
        bool allowed = voting.MayReplace(new SeatId(asking));

        // Assert
        Assert.Equal(mayReplace, allowed);
    }
}
