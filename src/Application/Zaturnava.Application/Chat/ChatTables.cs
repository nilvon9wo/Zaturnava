using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;

namespace Net.NowhereAtAll.Zaturnava.Application.Chat;

/// <summary>
/// The tables games are played at, and when a player at each was last heard from. A table is known by its first
/// game; a game played again after another is at the same table, so the talk goes on. Kept in memory, one per
/// host: after a restart, every table counts as heard from when the host began.
/// </summary>
public sealed class ChatTables(DecisionClock clock, ChatSettings settings)
{
    private readonly DecisionClock _clock = clock;
    private readonly ChatSettings _settings = settings;
    private readonly DateTimeOffset _opened = clock.Now;
    private readonly ConcurrentDictionary<GameId, GameId> _tables = new();
    private readonly ConcurrentDictionary<GameId, DateTimeOffset> _heard = new();

    /// <summary>The table the game is played at: its own, unless it was played again after another.</summary>
    public GameId TableOf([Required] GameId game) => this._tables.GetValueOrDefault(game, game);

    /// <summary>Has <paramref name="game"/>, played again after <paramref name="after"/>, at the same table.</summary>
    public void Continue([Required] GameId game, [Required] GameId after) =>
        this._tables[game] = this.TableOf(after);

    /// <summary>A player at the table was heard from just now.</summary>
    public void Heard([Required] GameId table) => this._heard[table] = this._clock.Now;

    /// <summary>
    /// Whether every player has left the table: none heard from for <see cref="ChatSettings.LeftAfter"/>.
    /// </summary>
    public bool IsLeft([Required] GameId table) =>
        this._clock.Now - this._heard.GetValueOrDefault(table, this._opened) >= this._settings.LeftAfter;

    /// <summary>Forgets the table, and that its games were played at it.</summary>
    public void Forget([Required] GameId table)
    {
        _ = this._heard.TryRemove(table, out _);
        foreach (GameId game in this._tables.Keys.ToList())
        {
            _ = this._tables.TryRemove(KeyValuePair.Create(game, table)); // only if it was played at this table
        }
    }
}
