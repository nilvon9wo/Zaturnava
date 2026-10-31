using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryChatStore : IChatPersisting
{
    private readonly ConcurrentDictionary<GameId, ImmutableList<ChatMessage>> _chats = new();

    public Task<ChatMessage> Add([Required] ChatMessage message, CancellationToken cancellationToken)
    {
        ImmutableList<ChatMessage> said = this._chats.AddOrUpdate(
            message.Table,
            _ => [message with { Number = 1 }],
            (_, before) => before.Add(message with { Number = before.Count + 1 })
        );
        return Task.FromResult(said[^1]);
    }

    public Task<ImmutableList<ChatMessage>> Of([Required] GameId table, CancellationToken cancellationToken) =>
        Task.FromResult(this._chats.GetValueOrDefault(table, []));

    public Task<ImmutableList<GameId>> Tables(CancellationToken cancellationToken) =>
        Task.FromResult<ImmutableList<GameId>>([.. this._chats.Keys]);

    public Task Remove([Required] GameId table, CancellationToken cancellationToken)
    {
        _ = this._chats.TryRemove(table, out _);
        return Task.CompletedTask;
    }
}
