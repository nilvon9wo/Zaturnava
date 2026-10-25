namespace Net.NowhereAtAll.Zaturnava.Hosting.Test;

public sealed class ListedPageHostTest
{
    [Fact]
    public void Origins_WhenListed_AreThoseListed()
    {
        // Arrange
        ListedPageHost host = new(["http://localhost:5165", "https://example.com"]);

        // Act
        IReadOnlyList<string> origins = host.Origins;

        // Assert
        Assert.Equal(["http://localhost:5165", "https://example.com"], origins);
    }
}
