namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class NonClosingWriterTest
{
    [Fact]
    public void Dispose_WhenDone_LeavesTheOwnersWriterOpen()
    {
        // Arrange
        using StringWriter owner = new();
        NonClosingWriter writer = new(owner);
        writer.Write("ab");
        writer.Write('c');

        // Act
        writer.Dispose();

        // Assert
        owner.Write('d');
        Assert.Equal(("abcd", owner.Encoding), (owner.ToString(), writer.Encoding));
    }
}
