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
}
