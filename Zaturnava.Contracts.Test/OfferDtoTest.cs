namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class OfferDtoTest
{
    [Fact]
    public void ActionOf_WhenGivenPrisoners_ListsThemAsTheOfferActionsArguments()
    {
        // Arrange - nothing beyond the prisoners

        // Act
        ActionDto action = OfferDto.ActionOf(0, 1, [9, 10], []);

        // Assert
        Assert.Equal(
            ("offer", "0", "1", "9,10", string.Empty),
            (
                action.Kind,
                action.Arguments[ActionArguments.Actor],
                action.Arguments[OfferDto.ReceiverArgument],
                action.Arguments[OfferDto.GivenArgument],
                action.Arguments[OfferDto.AskedArgument]
            )
        );
    }
}
