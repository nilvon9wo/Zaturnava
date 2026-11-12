using System.ComponentModel.DataAnnotations;
using System.Text;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

/// <summary>
/// Not a test of the code, but of the engine itself, where it is (<see cref="EnginePath"/>): Fairy-Stockfish plays
/// two seats of a short game of Chaturaji 2027, the Random bot the others, and all that was said between them is
/// written to <c>transcript.txt</c> in its <c>variants</c> folder. Without the engine, as on CI, it is skipped.
/// </summary>
public sealed class LiveEngineRun
{
    [Fact]
    public async Task Decide_WhenTheEngineIsHere_ChoosesMovesItNames()
    {
        // Arrange
        string? path = EnginePath.Find(
            Environment.GetEnvironmentVariable(EnginePath.Variable),
            new DirectoryInfo(AppContext.BaseDirectory)
        );
        Assert.SkipWhen(path is null, "Fairy-Stockfish is not here.");
        string folder = Path.Combine(Path.GetDirectoryName(path)!, "variants");
        RecordingChannel channel = new(new ProcessUciChannel(path, folder));
        using UciEngine engine = new(channel, folder);
        CountingBot fallback = new(new RandomBot(new Random(1)));
        FairyStockfishBot bot = new(engine, fallback);

        // Act
        try
        {
            _ = await DecisionLoop.PlayAll(
                new Chaturaji2027Rules(),
                [bot, new RandomBot(new Random(2)), bot, new RandomBot(new Random(3))],
                new RandomDice(new Random(4)),
                40,
                TestContext.Current.CancellationToken
            );
        }
        finally
        {
            await File.WriteAllTextAsync(
                Path.Combine(folder, "transcript.txt"),
                channel.Transcript,
                TestContext.Current.CancellationToken
            );
        }

        // Assert - the engine named a move at least once, rather than leaving every one to the fallback
        Assert.Contains("bestmove", channel.Transcript, StringComparison.Ordinal);
        Assert.True(channel.Named > 0, $"The engine named none of its moves; the fallback decided {fallback.Asked}.");
    }
}

/// <summary>
/// How Fairy-Stockfish turns a Black piece round: a piece defined to step right (<c>mrW</c>), Black's alone on d4,
/// Black to move; its moves listed (<c>go perft 1</c>) to <c>bearing.txt</c> in the <c>variants</c> folder. Without
/// the engine, it is skipped.
/// </summary>
public sealed class LiveBearingRun
{
    [Fact]
    public async Task Perft_WhenBlackStepsRightOrForward_GoesWestOrDownTurnedRightRound()
    {
        // Arrange
        string? path = EnginePath.Find(
            Environment.GetEnvironmentVariable(EnginePath.Variable),
            new DirectoryInfo(AppContext.BaseDirectory)
        );
        Assert.SkipWhen(path is null, "Fairy-Stockfish is not here.");
        string folder = Path.Combine(Path.GetDirectoryName(path)!, "variants");
        _ = Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(
            Path.Combine(folder, "bearing.ini"),
            "[bearing]\nmaxFile = 8\nmaxRank = 8\npawn = -\nknight = -\nbishop = -\nrook = -\nqueen = -\nking = -\n"
                + "castling = false\nextinctionValue = loss\nextinctionPieceTypes = *\ncustomPiece1 = a:mrW\n"
                + "customPiece2 = b:mfW\n",
            TestContext.Current.CancellationToken
        );
        using RecordingChannel channel = new(new ProcessUciChannel(path, folder));
        CancellationToken cancellation = TestContext.Current.CancellationToken;

        // Act
        foreach (string line in (string[])
            [
                "uci", "setoption name VariantPath value bearing.ini", "setoption name UCI_Variant value bearing",
                "position fen 7B/8/8/8/3a4/8/1b6/8 b - - 0 1", "go perft 1", "isready",
            ])
        {
            await channel.Send(line, cancellation);
        }

        string? said = await channel.Receive(cancellation);
        while (said is not null && !said.StartsWith("Nodes searched", StringComparison.Ordinal))
        {
            said = await channel.Receive(cancellation);
        }

        await File.WriteAllTextAsync(Path.Combine(folder, "bearing.txt"), channel.Transcript, cancellation);

        // Assert - turned right round: right is west, and forward is down
        Assert.Equal(
            (true, true),
            (
                channel.Transcript.Contains("d4c4: 1", StringComparison.Ordinal),
                channel.Transcript.Contains("b2b1: 1", StringComparison.Ordinal)
            )
        );
    }
}

/// <summary>A channel that writes down every line, and counts the moves the engine named.</summary>
internal sealed class RecordingChannel(IUciChannel inner) : IUciChannel
{
    private readonly IUciChannel _inner = inner;
    private readonly StringBuilder _transcript = new();

    public string Transcript => this._transcript.ToString();

    public int Named { get; private set; }

    public async Task Send([Required] string line, CancellationToken cancellationToken)
    {
        _ = this._transcript.Append("> ").AppendLine(line);
        await this._inner.Send(line, cancellationToken);
    }

    public async Task<string?> Receive(CancellationToken cancellationToken)
    {
        string? line = await this._inner.Receive(cancellationToken);
        _ = this._transcript.Append("< ").AppendLine(line);
        bool isNamed = line?.StartsWith("bestmove ", StringComparison.Ordinal) == true && !line.Contains("(none)");
        this.Named += isNamed ? 1 : 0;
        return line;
    }

    public void Dispose() => this._inner.Dispose();
}

/// <summary>A bot that counts how often it is asked.</summary>
internal sealed class CountingBot(IBotPlaying inner) : IBotPlaying
{
    private readonly IBotPlaying _inner = inner;

    public int Asked { get; private set; }

    public BotProfile Profile => this._inner.Profile;

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        this.Asked++;
        return this._inner.Decide(request, cancellationToken);
    }
}
