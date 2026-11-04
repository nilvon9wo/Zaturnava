namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class TimeAgoTest
{
    [Theory]
    [InlineData(0, "just now")]
    [InlineData(59, "just now")]
    [InlineData(60, "1 min ago")]
    [InlineData(3_599, "59 min ago")]
    [InlineData(3_600, "1 h ago")]
    [InlineData(86_399, "23 h ago")]
    [InlineData(86_400, "1 day ago")]
    [InlineData(172_799, "1 day ago")]
    [InlineData(172_800, "2 days ago")]
    public void Since_WhenSoLongAgo_SaysItRoughly(int seconds, string expected)
    {
        // Arrange
        DateTimeOffset now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        // Act
        string ago = TimeAgo.Since(now.AddSeconds(-seconds), now);

        // Assert
        Assert.Equal(expected, ago);
    }
}
