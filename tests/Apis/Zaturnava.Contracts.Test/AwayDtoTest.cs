namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class AwayDtoTest
{
    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void ActionOf_WhenVoting_SaysWhoVotesOnWhomAndHow(bool stops, string expected)
    {
        // Arrange - nothing beyond the vote

        // Act
        ActionDto action = AwayDto.ActionOf(1, 2, stops);

        // Assert
        Assert.Equal(
            ("vote-away", "1", "2", expected),
            (
                action.Kind,
                action.Arguments[ActionArguments.Actor],
                action.Arguments[AwayDto.AbsentArgument],
                action.Arguments[AwayDto.StopsArgument]
            )
        );
    }

    [Theory]
    [InlineData(1, true, 0, true)]
    [InlineData(1, false, 0, false)]
    [InlineData(0, true, 0, false)]
    [InlineData(2, true, 30, false)]
    [InlineData(2, true, 0, true)]
    public void MayReplace_WhenAsked_AllowsAVoterOnceOpenFirstOrAfterTheWait(
        int asking,
        bool isOpen,
        double secondsToWait,
        bool mayReplace
    )
    {
        // Arrange
        AwayDto away = new(0, isOpen, [1, 2], [], FirstArrival: 1, SecondsUntilAnyoneReplaces: secondsToWait);

        // Act
        bool allowed = away.MayReplace(asking);

        // Assert
        Assert.Equal(mayReplace, allowed);
    }
}
