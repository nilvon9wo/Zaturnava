using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryBotMatchupStore : IBotMatchupPersisting
{
    private readonly ConcurrentDictionary<(string Ruleset, string First, string Second), BotMatchup> _matchups = new();

    public Task<ImmutableList<BotMatchup>> Matchups(CancellationToken cancellationToken) =>
        Task.FromResult<ImmutableList<BotMatchup>>([.. this._matchups.Values]);

    public Task Add([Required] BotMatchup games, CancellationToken cancellationToken)
    {
        _ = this._matchups.AddOrUpdate(
            (games.Ruleset, games.First, games.Second),
            games,
            (_, kept) => kept with
            {
                Won = kept.Won + games.Won,
                Lost = kept.Lost + games.Lost,
                Drawn = kept.Drawn + games.Drawn,
            }
        );
        return Task.CompletedTask;
    }
}
