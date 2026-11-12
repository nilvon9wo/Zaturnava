namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

public sealed class UciEngineTest
{
    private static readonly FairyPosition Position = new("tiny", "[tiny]\nmaxFile = 2\n", "2/2 w - - 0 1");

    [Fact]
    public async Task BestMove_WhenAskedTwice_StartsAndLoadsTheVariantOnce()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ScriptedChannel channel = new("id name Fairy-Stockfish", "uciok", "readyok", "info depth 1", "bestmove a1a2");
        using UciEngine engine = new(channel, folder);
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        TimeSpan thinking = TimeSpan.FromMilliseconds(50);
        string? first = await engine.BestMove(Position, ["a1a2", "a1b1"], thinking, cancellation);
        channel.Reply("readyok", "bestmove a1b1 ponder a2a1");

        // Act
        string? second = await engine.BestMove(Position, ["a1a2", "a1b1"], thinking, cancellation);

        // Assert
        Assert.Equal(
            (
                "a1a2",
                "a1b1",
                "uci|setoption name VariantPath value tiny.ini|isready|setoption name UCI_Variant value tiny"
                    + "|position fen 2/2 w - - 0 1|go movetime 50 searchmoves a1a2 a1b1|isready"
                    + "|setoption name UCI_Variant value tiny|position fen 2/2 w - - 0 1"
                    + "|go movetime 50 searchmoves a1a2 a1b1",
                Position.Definition
            ),
            (
                first,
                second,
                string.Join('|', channel.Sent),
                await File.ReadAllTextAsync(Path.Combine(folder, "tiny.ini"), cancellation)
            )
        );
    }

    [Fact]
    public async Task BestMove_WhenTheEngineNamesAnotherMove_NamesNone()
    {
        // Arrange
        using UciEngine engine = new(new ScriptedChannel("uciok", "readyok", "bestmove b2b1"), Path.GetTempPath());

        // Act
        string? best = await engine.BestMove(
            Position with { Name = Guid.NewGuid().ToString("N") },
            ["a1a2"],
            TimeSpan.FromMilliseconds(50),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Null(best);
    }

    [Fact]
    public async Task BestMove_WhenTheEngineStopsBeforeNamingAMove_NamesNone()
    {
        // Arrange
        using UciEngine engine = new(new ScriptedChannel("uciok", "readyok"), Path.GetTempPath());

        // Act
        string? best = await engine.BestMove(
            Position with { Name = Guid.NewGuid().ToString("N") },
            ["a1a2"],
            TimeSpan.FromMilliseconds(50),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Null(best);
    }

    [Fact]
    public void Dispose_WhenDone_ClosesTheChannel()
    {
        // Arrange
        ScriptedChannel channel = new();
        UciEngine engine = new(channel, Path.GetTempPath());

        // Act
        engine.Dispose();

        // Assert
        Assert.True(channel.IsDisposed);
    }
}
