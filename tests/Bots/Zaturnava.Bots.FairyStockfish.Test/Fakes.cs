using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

/// <summary>An engine that says <paramref name="replies"/>, in order, then nothing; and keeps what it hears.</summary>
internal sealed class ScriptedChannel(params string[] replies) : IUciChannel
{
    private readonly Queue<string> _replies = new(replies);

    public ImmutableList<string> Sent { get; private set; } = [];

    public bool IsDisposed { get; private set; }

    /// <summary>Whether it answers later, as a program does, rather than at once.</summary>
    public bool IsYielding { get; init; }

    /// <summary>Says more, after what it was to say.</summary>
    public void Reply(params string[] replies)
    {
        foreach (string reply in replies)
        {
            this._replies.Enqueue(reply);
        }
    }

    public async Task Send([Required] string line, CancellationToken cancellationToken)
    {
        await this.Later();
        this.Sent = this.Sent.Add(line);
    }

    public async Task<string?> Receive(CancellationToken cancellationToken)
    {
        await this.Later();
        return this._replies.TryDequeue(out string? line) ? line : null;
    }

    public void Dispose() => this.IsDisposed = true;

    private async Task Later()
    {
        if (this.IsYielding)
        {
            await Task.Delay(1);
        }
    }
}

/// <summary>
/// An 8 by 8 grid, but for its cells, <paramref name="cells"/> if given, and the directions it resolves every set to,
/// <paramref name="directions"/> if given.
/// </summary>
internal sealed class ShapedTopology(ImmutableList<CellId>? cells = null, ImmutableList<Direction>? directions = null)
    : ICellNavigating
{
    private readonly SquareGridTopology _grid = new(8, 8);

    public ImmutableList<CellId> Cells => cells ?? this._grid.Cells;

    public CellId? Step(CellId from, Direction direction) => this._grid.Step(from, direction);

    public ImmutableList<Direction> Resolve([Required] DirectionSet set, Direction forward) =>
        directions ?? this._grid.Resolve(set, forward);

    public ImmutableList<Direction> WaypointsOf(Direction leap) => this._grid.WaypointsOf(leap);

    public CellLayout LayoutOf(CellId cell) => this._grid.LayoutOf(cell);

    public string NameOf(CellId cell) => this._grid.NameOf(cell);
}

/// <summary>A bot that always takes the first legal action, and seizes <paramref name="seized"/>.</summary>
internal sealed class StubBot(GameAction? seized = null) : IBotPlaying
{
    public BotProfile Profile { get; } = new("stub", "Stub", "The first legal action.", UsesAi: false);

    public int Asked { get; private set; }

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
    {
        this.Asked++;
        return Task.FromResult(request.LegalActions[0]);
    }

    public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(seized);
}
