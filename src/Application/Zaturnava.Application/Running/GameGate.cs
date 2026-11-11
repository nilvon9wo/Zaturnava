using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>
/// Lets one request at a time change each game, on this host, in the order they came: the next waits until the one
/// before has saved, then reads what it saved, rather than racing it to save and being refused for it (Proposed). So
/// several bots, each letting the server's bots move as soon as they see it is their turn, no longer trip one another
/// up. One per host, in memory, as <see cref="DecisionClock"/> is; the store's version check still guards a game
/// changed by another host.
/// </summary>
public sealed class GameGate
{
    private readonly Lock _lock = new();
    private readonly Dictionary<GameId, Passage> _last = [];

    /// <summary>
    /// Waits for every request already let through to change <paramref name="game"/> to be done with it; the game is
    /// then this request's to change, until it disposes of what this returns.
    /// </summary>
    public async Task<IDisposable> Enter([Required] GameId game)
    {
        Passage passage = new(this, game);
        Task before;
        lock (this._lock)
        {
            before = this._last.TryGetValue(game, out Passage? last) ? last.Done : Task.CompletedTask;
            this._last[game] = passage;
        }

        await before;
        return passage;
    }

    /// <summary>How many games requests are changing, or waiting to: none are kept once all are done.</summary>
    public int Busy
    {
        get
        {
            lock (this._lock)
            {
                return this._last.Count;
            }
        }
    }

    private void Leave(GameId game, Passage passage)
    {
        lock (this._lock)
        {
            if (this._last.GetValueOrDefault(game) == passage)
            {
                _ = this._last.Remove(game);
            }
        }
    }

    /// <summary>One request's turn at a game: done once disposed of.</summary>
    private sealed class Passage(GameGate gate, GameId game) : IDisposable
    {
        private readonly GameGate _gate = gate;
        private readonly GameId _game = game;
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Done => this._done.Task;

        public void Dispose()
        {
            this._gate.Leave(this._game, this);
            _ = this._done.TrySetResult();
        }
    }
}
