namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class EventDtoTest
{
    [Theory]
    [InlineData(EventDto.Captured, true)]
    [InlineData(EventDto.Ended, true)]
    [InlineData(EventDto.Moved, false)]
    [InlineData(EventDto.Rolled, false)]
    public void Highlights_WhenAskedOfAKind_HoldThoseWorthKeeping(string kind, bool expected)
    {
        // Arrange - nothing beyond the kind

        // Act
        bool isHighlight = EventDto.Highlights.Contains(kind);

        // Assert
        Assert.Equal(expected, isHighlight);
    }
}
