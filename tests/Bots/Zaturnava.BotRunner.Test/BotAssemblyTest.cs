using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

public sealed class BotAssemblyTest
{
    [Fact]
    public void BotsIn_WhenTheAssemblyHasBots_MakesEachThatCanBeMade()
    {
        // Arrange - this assembly, with its own bots
        string path = typeof(SeededOwnBot).Assembly.Location;

        // Act
        ImmutableList<IBotPlaying> bots = BotAssembly.BotsIn(path, new Random(1));

        // Assert
        Assert.Equal(
            [PlainOwnBot.BotId, SeededOwnBot.BotId],
            bots.Select(bot => bot.Profile.Id).Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void BotsIn_WhenThereIsNoFile_ThrowsArgumentException()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.dll");

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BotAssembly.BotsIn(path, new Random(1)));

        // Assert
        Assert.StartsWith($"No bots could be loaded from '{path}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BotsIn_WhenTheFileIsNoAssembly_ThrowsArgumentException()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"zaturnava-{Guid.NewGuid():N}.dll");
        File.WriteAllText(path, "not an assembly");

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => BotAssembly.BotsIn(path, new Random(1)));

        // Assert
        File.Delete(path);
        Assert.StartsWith($"No bots could be loaded from '{path}'", exception.Message, StringComparison.Ordinal);
    }
}
