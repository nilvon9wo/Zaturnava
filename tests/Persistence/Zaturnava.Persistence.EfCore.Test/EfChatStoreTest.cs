using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class EfChatStoreTest : IDisposable
{
    private static readonly GameId Game = new("game");
    private static readonly DateTimeOffset At = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteGameDatabase _database = new();
    private readonly EfChatStore _store;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public EfChatStoreTest() => this._store = new EfChatStore(this._database.Context);

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task Add_WhenMessagesAreSaid_NumbersThemInOrderWithinTheirGame()
    {
        // Arrange
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        _ = await this._store.Add(Message(new GameId("other"), "elsewhere"), this._cancellation);

        // Act
        ChatMessage second =
            await new EfChatStore(this._database.NewContext()).Add(Message(Game, "second"), this._cancellation);

        // Assert
        Assert.Equal(Message(Game, "second") with { Number = 2 }, second);
    }

    [Fact]
    public async Task Of_WhenMessagesWereSaid_ReadsTheGamesBackInOrder()
    {
        // Arrange
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        _ = await this._store.Add(Message(new GameId("other"), "elsewhere"), this._cancellation);
        _ = await this._store.Add(Message(Game, "second") with { Channel = ChatChannel.Table }, this._cancellation);

        // Act
        ImmutableList<ChatMessage> said =
            await new EfChatStore(this._database.NewContext()).Of(Game, this._cancellation);

        // Assert
        Assert.Equal(
            [Message(Game, "first") with { Number = 1 }, Message(Game, "second") with
            {
                Number = 2,
                Channel = ChatChannel.Table,
            }],
            said
        );
    }

    [Fact]
    public async Task Add_WhenAnotherTookTheNumberMeanwhile_ThrowsConcurrencyConflictException()
    {
        // Arrange - the other message is about to be saved with the number this one will be given
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        GameDbContext secondRequest = this._database.NewContext();
        _ = secondRequest.Chats.Add(
            new ChatEntity
            {
                Id = "meanwhile",
                TableId = Game.Value,
                Number = 2,
                Seat = 0,
                Channel = nameof(ChatChannel.Table),
                Text = "meanwhile",
                At = At,
            }
        );

        // Act
        Task adding = new EfChatStore(secondRequest).Add(Message(Game, "second"), this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => adding);
    }

    [Fact]
    public async Task Remove_WhenATableIsLetGo_KeepsOnlyTheOthers()
    {
        // Arrange
        GameId other = new("other");
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        _ = await this._store.Add(Message(Game, "second"), this._cancellation);
        _ = await this._store.Add(Message(other, "elsewhere"), this._cancellation);

        // Act
        await new EfChatStore(this._database.NewContext()).Remove(Game, this._cancellation);

        // Assert
        EfChatStore reader = new(this._database.NewContext());
        Assert.Equal(
            (other, 0),
            (
                (await reader.Tables(this._cancellation)).Single(),
                (await reader.Of(Game, this._cancellation)).Count
            )
        );
    }

    private static ChatMessage Message(GameId game, string text) =>
        new(game, 0, new SeatId(1), ChatChannel.Team, text, At);
}
