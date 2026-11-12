namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

public sealed class ProcessUciChannelTest
{
    [Fact]
    public async Task Receive_WhenTheProgramEchoesWhatItIsSent_HearsItBack()
    {
        // Arrange - cat says back each line it is sent, and runs until it is stopped
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using ProcessUciChannel channel = new("cat", folder);
        await channel.Send("uci", TestContext.Current.CancellationToken);

        // Act
        string? heard = await channel.Receive(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(("uci", true), (heard, Directory.Exists(folder)));
    }
}

public sealed class LazyUciChannelTest
{
    [Fact]
    public void Dispose_WhenNeverUsed_NeverOpensTheChannel()
    {
        // Arrange
        int opened = 0;
        LazyUciChannel channel = new(() =>
        {
            opened++;
            return new ScriptedChannel();
        });

        // Act
        channel.Dispose();

        // Assert
        Assert.Equal(0, opened);
    }

    [Fact]
    public async Task Dispose_WhenUsed_ClosesTheChannelItOpenedOnce()
    {
        // Arrange
        int opened = 0;
        ScriptedChannel inner = new("uciok");
        LazyUciChannel channel = new(() =>
        {
            opened++;
            return inner;
        });
        await channel.Send("uci", TestContext.Current.CancellationToken);
        string? heard = await channel.Receive(TestContext.Current.CancellationToken);

        // Act
        channel.Dispose();

        // Assert
        Assert.Equal((1, "uciok", "uci", true), (opened, heard, inner.Sent.Single(), inner.IsDisposed));
    }
}

public sealed class EnginePathTest
{
    [Fact]
    public void Find_WhenTheVariableSaysWhere_IsThere()
    {
        // Arrange
        DirectoryInfo start = new(Path.GetTempPath());

        // Act
        string? path = EnginePath.Find("/opt/fairy-stockfish", start);

        // Assert
        Assert.Equal("/opt/fairy-stockfish", path);
    }

    [Fact]
    public void Find_WhenANearerFolderHasNone_FindsTheFirstOfTheNextOneUp()
    {
        // Arrange - the root's folder has two, one deeper; the inner folder's has none
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string engines = Path.Combine(root, EnginePath.Folder);
        _ = Directory.CreateDirectory(Path.Combine(engines, "build"));
        File.WriteAllText(Path.Combine(engines, "fairy-stockfish-b"), string.Empty);
        File.WriteAllText(Path.Combine(engines, "build", "fairy-stockfish-a"), string.Empty);
        File.WriteAllText(Path.Combine(engines, "readme.txt"), string.Empty);
        string inner = Path.Combine(root, "inner");
        _ = Directory.CreateDirectory(Path.Combine(inner, EnginePath.Folder));
        _ = Directory.CreateDirectory(Path.Combine(inner, "bin"));

        // Act
        string? path = EnginePath.Find(null, new DirectoryInfo(Path.Combine(inner, "bin")));

        // Assert
        Assert.Equal(Path.Combine(engines, "build", "fairy-stockfish-a"), path);
    }

    [Fact]
    public void Find_WhenNoFolderUpHasIt_FindsNone()
    {
        // Arrange
        DirectoryInfo top = new(Path.GetPathRoot(Path.GetTempPath())!);

        // Act
        string? path = EnginePath.Find(null, top);

        // Assert
        Assert.Null(path);
    }
}
