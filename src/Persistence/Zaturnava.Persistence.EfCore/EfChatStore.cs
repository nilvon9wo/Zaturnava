using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>
/// What is said at each table, in a relational database. Each table's messages are numbered in order, and the
/// number is unique within the table, so two said at once cannot both take it.
/// </summary>
public sealed class EfChatStore(GameDbContext context) : IChatPersisting
{
    private readonly GameDbContext _context = context;

    /// <exception cref="ConcurrencyConflictException">Another message took the number meanwhile.</exception>
    public async Task<ChatMessage> Add([Required] ChatMessage message, CancellationToken cancellationToken)
    {
        int last = await this._context.Chats
            .Where(chat => chat.TableId == message.Table.Value)
            .MaxAsync(chat => (int?)chat.Number, cancellationToken) ?? 0;
        ChatMessage numbered = message with { Number = last + 1 };
        _ = this._context.Chats.Add(ToEntity(numbered));
        try
        {
            _ = await this._context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            this._context.ChangeTracker.Clear();
            throw new ConcurrencyConflictException(message.Table, exception);
        }

        return numbered;
    }

    public async Task<ImmutableList<ChatMessage>> Of([Required] GameId table, CancellationToken cancellationToken)
    {
        List<ChatEntity> entities = await this._context.Chats
            .Where(chat => chat.TableId == table.Value)
            .OrderBy(chat => chat.Number)
            .ToListAsync(cancellationToken);
        return [.. entities.Select(ToMessage)];
    }

    public async Task<ImmutableList<GameId>> Tables(CancellationToken cancellationToken)
    {
        List<string> tables = await this._context.Chats
            .Select(chat => chat.TableId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return [.. tables.Select(table => new GameId(table))];
    }

    public Task Remove([Required] GameId table, CancellationToken cancellationToken) =>
        this._context.Chats.Where(chat => chat.TableId == table.Value).ExecuteDeleteAsync(cancellationToken);

    private static ChatEntity ToEntity(ChatMessage message) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            TableId = message.Table.Value,
            Number = message.Number,
            Seat = message.From.Value,
            Channel = message.Channel.ToString(),
            Text = message.Text,
            At = message.At,
        };

    private static ChatMessage ToMessage(ChatEntity entity) =>
        new(
            new GameId(entity.TableId),
            entity.Number,
            new SeatId(entity.Seat),
            Enum.Parse<ChatChannel>(entity.Channel),
            entity.Text,
            entity.At
        );
}
