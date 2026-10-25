namespace Net.NowhereAtAll.Zaturnava.Hosting.Itch.Test;

public sealed class ItchPageHostTest
{
    [Fact]
    public void Origins_WhenServedByItchIo_AreItsContentDomains()
    {
        // Arrange
        ItchPageHost host = new();

        // Act
        IReadOnlyList<string> origins = host.Origins;

        // Assert
        Assert.Equal(["https://html-classic.itch.zone", "https://html.itch.zone"], origins);
    }
}
