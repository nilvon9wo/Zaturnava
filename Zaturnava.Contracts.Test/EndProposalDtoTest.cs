namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class EndProposalDtoTest
{
    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void ActionOf_WhenProposing_SaysWhoProposesAndWhetherTheyGiveUpTheirTurns(bool resigns, string expected)
    {
        // Arrange - nothing beyond the proposer

        // Act
        ActionDto action = EndProposalDto.ActionOf(2, resigns);

        // Assert
        Assert.Equal(
            ("propose-end", "2", expected),
            (action.Kind, action.Arguments[ActionArguments.Actor], action.Arguments[EndProposalDto.ResignsArgument])
        );
    }
}
