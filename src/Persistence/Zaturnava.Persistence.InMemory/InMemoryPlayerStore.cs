using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryPlayerStore : IPlayerPersisting
{
    private readonly ConcurrentDictionary<PlayerId, Player> _players = new();
    private readonly ConcurrentDictionary<string, PlayerId> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<PlayerId, DateTimeOffset> _signedIn = new();
    private readonly ConcurrentDictionary<(PlayerId Player, string Game), Standing> _standings = new();

    public Task<Player?> ByLogin(Login login, CancellationToken cancellationToken) =>
        Task.FromResult(this._players.Values.FirstOrDefault(player => player.Logins.Contains(login)));

    public Task<Player?> ById(PlayerId player, CancellationToken cancellationToken) =>
        Task.FromResult(this._players.GetValueOrDefault(player));

    public Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(
            this._sessions.TryGetValue(tokenHash, out PlayerId player)
                ? this._players.GetValueOrDefault(player)
                : null
        );

    public Task<ImmutableDictionary<PlayerId, string>> Names(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            players
                .Distinct()
                .Select(this._players.GetValueOrDefault)
                .OfType<Player>()
                .ToImmutableDictionary(player => player.Id, player => player.Name)
        );

    public Task<ImmutableDictionary<PlayerId, DateTimeOffset>> LastSignedIn(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            players
                .Distinct()
                .Where(this._signedIn.ContainsKey)
                .ToImmutableDictionary(player => player, player => this._signedIn[player])
        );

    public Task MarkSignedIn(PlayerId player, DateTimeOffset time, CancellationToken cancellationToken)
    {
        this._signedIn[player] = time;
        return Task.CompletedTask;
    }

    public Task Add([Required] Player player, CancellationToken cancellationToken)
    {
        this._players[player.Id] = player;
        return Task.CompletedTask;
    }

    public Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Name = name });

    public Task KeepPreferences(
        PlayerId player,
        [Required] ImmutableSortedDictionary<string, string> preferences,
        CancellationToken cancellationToken
    ) =>
        this.Change(player, known => known with { Preferences = preferences });

    public Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Logins = known.Logins.Add(login) });

    public Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Logins = known.Logins.Remove(login) });

    public Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken)
    {
        this._sessions[tokenHash] = player;
        return Task.CompletedTask;
    }

    public Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken)
    {
        _ = this._sessions.TryRemove(tokenHash, out _);
        return Task.CompletedTask;
    }

    public Task Merge(PlayerId from, PlayerId into, CancellationToken cancellationToken)
    {
        _ = this.Change(into, known => known with { Logins = known.Logins.AddRange(this._players[from].Logins) });
        ImmutableList<string> sessions =
            [.. this._sessions.Where(session => session.Value == from).Select(session => session.Key)];
        sessions.ForEach(session => this._sessions[session] = into);
        ImmutableList<Standing> standings = [.. this._standings.Values.Where(standing => standing.Player == from)];
        standings.ForEach(standing => this._standings[(into, standing.Game)] =
            this._standings.GetValueOrDefault((into, standing.Game)) is { } mine
                ? mine.Merged(standing)
                : standing with { Player = into }
        );
        return this.Remove(from, cancellationToken);
    }

    public Task Remove(PlayerId player, CancellationToken cancellationToken)
    {
        _ = this._players.TryRemove(player, out _);
        _ = this._signedIn.TryRemove(player, out _);
        ImmutableList<string> sessions =
            [.. this._sessions.Where(session => session.Value == player).Select(session => session.Key)];
        sessions.ForEach(session => _ = this._sessions.TryRemove(session, out _));
        ImmutableList<(PlayerId, string)> standings = [.. this._standings.Keys.Where(key => key.Player == player)];
        standings.ForEach(standing => _ = this._standings.TryRemove(standing, out _));
        return Task.CompletedTask;
    }

    public Task<ImmutableDictionary<PlayerId, Standing>> Standings(
        [Required] string game,
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            players
                .Distinct()
                .Select(player => this._standings.GetValueOrDefault((player, game)))
                .OfType<Standing>()
                .ToImmutableDictionary(standing => standing.Player)
        );

    public Task Keep([Required] ImmutableList<Standing> standings, CancellationToken cancellationToken)
    {
        standings.ForEach(standing => this._standings[(standing.Player, standing.Game)] = standing);
        return Task.CompletedTask;
    }

    public Task<ImmutableList<Standing>> Leaders(
        [Required] string game,
        int count,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<Standing> leaders =
        [
            .. this._standings.Values
                .Where(standing => standing.Game == game)
                .OrderByDescending(standing => standing.Elo)
                .ThenByDescending(standing => standing.Experience)
                .ThenBy(standing => standing.Player.Value, StringComparer.Ordinal)
                .Take(count),
        ];
        return Task.FromResult(leaders);
    }

    /// <summary>Changes the player, if still known.</summary>
    private Task Change(PlayerId player, Func<Player, Player> change)
    {
        if (this._players.TryGetValue(player, out Player? known))
        {
            this._players[player] = change(known);
        }

        return Task.CompletedTask;
    }
}
