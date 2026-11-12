using System.Collections.Immutable;
using System.ComponentModel;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

public sealed class FairyStockfishBotTest
{
    private static readonly ImmutableList<PieceKind> Kinds =
        [new(GridRules.King, [MovementRule.Step(DirectionSets.Any)])];

    private static readonly SeatId Red = new(0);

    /// <summary>Red's King on e1 may go to e2 or d1 (cells 12 and 3); Blue's is on e8.</summary>
    private static readonly ImmutableList<GameAction> KingMoves =
        [new MoveAction(Red, new CellId(4), new CellId(12)), new MoveAction(Red, new CellId(4), new CellId(3))];

    private static readonly FairyPosition Tiny = new("tiny", "[tiny]\n", "2/2 w - - 0 1");

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenThereIsOneMove_LeavesItToTheFallback()
    {
        // Arrange
        ScriptedChannel channel = new();
        StubBot fallback = new();
        FairyStockfishBot bot = new(new UciEngine(channel, Path.GetTempPath()), fallback);

        // Act
        GameAction action = await bot.Decide(Request(new GridRules(Kinds), [KingMoves[0]]), Cancellation);

        // Assert
        Assert.Equal((KingMoves[0], 1, 0), (action, fallback.Asked, channel.Sent.Count));
    }

    [Fact]
    public async Task Decide_WhenTheEngineCannotSeeTheGame_LeavesItToTheFallback()
    {
        // Arrange
        ScriptedChannel channel = new();
        StubBot fallback = new();
        FairyStockfishBot bot = new(new UciEngine(channel, Path.GetTempPath()), fallback);

        // Act
        GameAction action = await bot.Decide(Request(new GridRules(Kinds, 13), KingMoves), Cancellation);

        // Assert
        Assert.Equal((KingMoves[0], 1, 0), (action, fallback.Asked, channel.Sent.Count));
    }

    [Fact]
    public async Task Decide_WhenTheEngineNamesAMove_MakesIt()
    {
        // Arrange
        StubBot fallback = new();
        FairyStockfishBot bot = new(
            new UciEngine(new ScriptedChannel("uciok", "readyok", "bestmove e1d1"), Path.GetTempPath()),
            fallback
        );

        // Act
        GameAction action = await bot.Decide(Request(new GridRules(Kinds), KingMoves), Cancellation);

        // Assert
        Assert.Equal((KingMoves[1], 0), (action, fallback.Asked));
    }

    [Fact]
    public async Task Decide_WhenTheEngineNamesAMoveLater_MakesIt()
    {
        // Arrange
        ScriptedChannel channel = new("uciok", "readyok", "bestmove e1e2") { IsYielding = true };
        FairyStockfishBot bot = new(new UciEngine(channel, Path.GetTempPath()), new StubBot());

        // Act
        GameAction action = await bot.Decide(Request(new GridRules(Kinds), KingMoves), Cancellation);

        // Assert
        Assert.Equal(KingMoves[0], action);
    }

    [Fact]
    public async Task Decide_WhenTheEngineNamesNone_LeavesItToTheFallback()
    {
        // Arrange
        StubBot fallback = new();
        FairyStockfishBot bot = new(
            new UciEngine(new ScriptedChannel("uciok", "readyok"), Path.GetTempPath()),
            fallback
        );

        // Act
        GameAction action = await bot.Decide(Request(new GridRules(Kinds), KingMoves), Cancellation);

        // Assert
        Assert.Equal((KingMoves[0], 1), (action, fallback.Asked));
    }

    [Fact]
    public async Task Seize_WhenAsked_LeavesItToTheFallback()
    {
        // Arrange
        PassAction seized = new(Red);
        FairyStockfishBot bot = new(new UciEngine(new ScriptedChannel(), Path.GetTempPath()), new StubBot(seized));

        // Act
        GameAction? action = await bot.Seize(Request(new GridRules(Kinds), KingMoves), Cancellation);

        // Assert
        Assert.Same(seized, action);
    }

    [Fact]
    public void Profile_WhenLoadedAsABotAssembly_IsFairyStockfish()
    {
        // Arrange
        FairyStockfishBot bot = new(new Random(1));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((FairyStockfishBot.BotId, false), (profile.Id, profile.UsesAi));
    }

    [Fact]
    public async Task EngineAt_WhenThereIsNoEngine_FailsOnceAsked()
    {
        // Arrange
        using UciEngine engine = FairyStockfishBot.EngineAt(null);

        // Act
        Task<string?> asking = engine.BestMove(Tiny, ["a1a2"], FairyStockfishBot.Thinking, Cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<FileNotFoundException>(() => asking);
    }

    [Fact]
    public async Task EngineAt_WhenTheEngineWillNotStart_FailsOnceAsked()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "fairy-stockfish");
        using UciEngine engine = FairyStockfishBot.EngineAt(path);

        // Act
        Task<string?> asking = engine.BestMove(Tiny, ["a1a2"], FairyStockfishBot.Thinking, Cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<Win32Exception>(() => asking);
    }

    private static DecisionRequest Request(GridRules rules, ImmutableList<GameAction> legal) =>
        new(rules, GridRules.State((4, 0, "king", 0), (4, 7, "king", 1)), Red, legal);
}
