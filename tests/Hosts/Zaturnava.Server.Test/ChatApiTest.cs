using System.Collections.Immutable;
using System.Net.Http.Json;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Talk at a table, as a client sees it: a game whose first two seats (Black and Yellow, of opposing teams)
/// are people, and whose others are bots.
/// </summary>
public sealed class ChatApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly HttpClient _http = server.CreateClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Say_WhenAPlayerSpeaks_AnswersWithTheMessageNumbered()
    {
        // Arrange
        (string gameId, string black, _) = await this.SeatedGame();

        // Act
        ChatMessageDto said = await this._client.Say(
            gameId,
            black,
            new SayRequest(ChatChannels.Team, " Hold the left. "),
            this._cancellation
        );

        // Assert
        Assert.Equal((1, 0, ChatChannels.Team, "Hold the left."), (said.Number, said.Seat, said.Channel, said.Text));
    }

    [Fact]
    public async Task Chat_WhenEachReads_TheyGetTheTableAndTheirOwnTeamOnly()
    {
        // Arrange
        (string gameId, string black, string yellow) = await this.TalkedGame();

        // Act
        ImmutableList<ChatMessageDto> blackReads = await this._client.Chat(gameId, black, 0, this._cancellation);
        ImmutableList<ChatMessageDto> yellowReads = await this._client.Chat(gameId, yellow, 0, this._cancellation);
        ImmutableList<ChatMessageDto> watcherReads = await this._client.Chat(gameId, null, 0, this._cancellation);

        // Assert
        Assert.Equal(
            ("Hello|Ours", "Hello", "Hello"),
            (Texts(blackReads), Texts(yellowReads), Texts(watcherReads))
        );
    }

    [Fact]
    public async Task Chat_WhenAfterAMessage_GetsOnlyWhatCameLater()
    {
        // Arrange
        (string gameId, string black, _) = await this.TalkedGame();

        // Act
        ImmutableList<ChatMessageDto> read = await this._client.Chat(gameId, black, 1, this._cancellation);

        // Assert
        Assert.Equal("Ours", Texts(read));
    }

    [Fact]
    public async Task Chat_WhenNotToldWhereToStart_GetsEverything()
    {
        // Arrange
        (string gameId, _, _) = await this.TalkedGame();

        // Act
        ImmutableList<ChatMessageDto>? read =
            await this._http.GetFromJsonAsync<ImmutableList<ChatMessageDto>>(
                GameRoutes.Chat(gameId),
                this._cancellation
            );

        // Assert
        Assert.Equal("Hello", Texts(read!));
    }

    [Fact]
    public async Task Create_WhenPlayedAgainAfterAnotherGame_GoesOnWithItsTablesTalk()
    {
        // Arrange
        (string before, _, _) = await this.TalkedGame();
        CreateGameRequest request =
            new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat) { Continuing = before };

        // Act
        GameCreatedDto again = await this._client.Create(request, this._cancellation);

        // Assert
        ImmutableList<ChatMessageDto> read = await this._client.Chat(again.GameId, null, 0, this._cancellation);
        Assert.Equal("Hello", Texts(read));
    }

    [Fact]
    public async Task Say_WhenTheChannelIsUnknown_IsRefusedAsInvalid()
    {
        // Arrange
        (string gameId, string black, _) = await this.SeatedGame();

        // Act
        Task<ChatMessageDto> saying =
            this._client.Say(gameId, black, new SayRequest("everyone", "Hi"), this._cancellation);

        // Assert
        await AssertRefused(Refusal.Invalid, saying);
    }

    [Fact]
    public async Task Say_WhenTheTokenHoldsNoSeat_IsRefusedAsForbidden()
    {
        // Arrange
        (string gameId, _, _) = await this.SeatedGame();

        // Act
        Task<ChatMessageDto> saying =
            this._client.Say(gameId, "stranger", new SayRequest(ChatChannels.Table, "Hi"), this._cancellation);

        // Assert
        await AssertRefused(Refusal.Forbidden, saying);
    }

    [Fact]
    public async Task Say_WhenTheRequestCarriesNoToken_IsRefused()
    {
        // Arrange
        (string gameId, _, _) = await this.SeatedGame();

        // Act
        using HttpResponseMessage response = await this._http.PostAsJsonAsync(
            GameRoutes.Chat(gameId),
            new SayRequest(ChatChannels.Table, "Hi"),
            this._cancellation
        );

        // Assert
        Assert.False(response.IsSuccessStatusCode);
    }

    private static string Texts(ImmutableList<ChatMessageDto> said) =>
        string.Join("|", said.Select(message => message.Text));

    private static async Task AssertRefused<T>(Refusal expected, Task<T> request)
    {
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() => request);
        Assert.Equal(expected, refused.Refusal);
    }

    /// <summary>A game with people at Black and Yellow, and their tokens.</summary>
    private async Task<(string GameId, string Black, string Yellow)> SeatedGame()
    {
        CreateGameRequest request =
            new(ZaturnavaServer.Chaturaji2027, [null, null, ZaturnavaServer.Random, ZaturnavaServer.Random]);
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        SeatClaimedDto black = await this._client.Claim(created.GameId, 0, null, this._cancellation);
        SeatClaimedDto yellow = await this._client.Claim(created.GameId, 1, null, this._cancellation);
        return (created.GameId, black.Token, yellow.Token);
    }

    /// <summary>
    /// As <see cref="SeatedGame"/>, once Black has said "Hello" to the table, then "Ours" to their team.
    /// </summary>
    private async Task<(string GameId, string Black, string Yellow)> TalkedGame()
    {
        (string gameId, string black, string yellow) = await this.SeatedGame();
        _ = await this._client.Say(gameId, black, new SayRequest(ChatChannels.Table, "Hello"), this._cancellation);
        _ = await this._client.Say(gameId, black, new SayRequest(ChatChannels.Team, "Ours"), this._cancellation);
        return (gameId, black, yellow);
    }
}
