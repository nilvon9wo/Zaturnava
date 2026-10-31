using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory.Test;

public sealed class InMemoryChatStoreTest
{
    private static readonly GameId Game = new("game");

    private readonly InMemoryChatStore _store = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_WhenMessagesAreSaid_NumbersThemInOrderWithinTheirGame()
    {
        // Arrange
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        _ = await this._store.Add(Message(new GameId("other"), "elsewhere"), this._cancellation);

        // Act
        ChatMessage second = await this._store.Add(Message(Game, "second"), this._cancellation);

        // Assert
        Assert.Equal(Message(Game, "second") with { Number = 2 }, second);
    }

    [Fact]
    public async Task Of_WhenMessagesWereSaid_ReturnsTheGamesInOrder()
    {
        // Arrange
        _ = await this._store.Add(Message(Game, "first"), this._cancellation);
        _ = await this._store.Add(Message(new GameId("other"), "elsewhere"), this._cancellation);
        _ = await this._store.Add(Message(Game, "second"), this._cancellation);

        // Act
        ImmutableList<ChatMessage> said = await this._store.Of(Game, this._cancellation);

        // Assert
        Assert.Equal([(1, "first"), (2, "second")], said.Select(message => (message.Number, message.Text)));
    }

    [Fact]
    public async Task Of_WhenNothingWasSaid_ReturnsNothing()
    {
        // Arrange - nothing beyond the empty store

        // Act
        ImmutableList<ChatMessage> said = await this._store.Of(Game, this._cancellation);

        // Assert
        Assert.Empty(said);
    }

    private static ChatMessage Message(GameId game, string text) =>
        new(game, 0, new SeatId(1), ChatChannel.Team, text, DateTimeOffset.UnixEpoch);
}
