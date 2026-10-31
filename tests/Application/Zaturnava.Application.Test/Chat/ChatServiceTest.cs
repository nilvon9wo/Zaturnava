using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Chat;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Chat;

/// <summary>Talk at a game of <see cref="TinyRules"/>: two seats, each a team of its own.</summary>
public sealed class ChatServiceTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);

    private readonly SteppedTime _time = new();
    private readonly GameService _games;
    private readonly ChatService _chat;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public ChatServiceTest()
    {
        this._games = new GameService(
            new RulesetCatalog([new TinyRules(6)]),
            new BotCatalog([new FirstChoiceBot()]),
            ScenarioCatalog.None,
            new InMemoryGameStore(),
            new RecordingNotifier(),
            new LoadedDice(4),
            new DecisionClock(this._time),
            GameServiceSettings.Default
        );
        this._chat = new ChatService(this._games, new InMemoryChatStore(), new DecisionClock(this._time));
    }

    [Fact]
    public async Task Say_WhenAPlayerSpeaks_KeepsTheWordsTrimmedNumberedAndTimed()
    {
        // Arrange
        (GameId id, string first, _) = await this.Seated();

        // Act
        ChatMessage said = await this._chat.Say(id, first, ChatChannel.Table, "  Good luck!  ", this._cancellation);

        // Assert
        Assert.Equal(new ChatMessage(id, 1, First, ChatChannel.Table, "Good luck!", this._time.Now), said);
    }

    [Fact]
    public async Task Say_WhenTheTokenHoldsNoSeat_ThrowsNotSeatedException()
    {
        // Arrange
        (GameId id, _, _) = await this.Seated();

        // Act
        Task<ChatMessage> saying = this._chat.Say(id, "stranger", ChatChannel.Table, "Hello", this._cancellation);

        // Assert
        NotSeatedException refused = await Assert.ThrowsAsync<NotSeatedException>(() => saying);
        Assert.Equal(GameRequestFailure.Forbidden, refused.Failure);
    }

    [Fact]
    public async Task Say_WhenTheWordsAreTooMany_ThrowsInvalidChatException()
    {
        // Arrange
        (GameId id, string first, _) = await this.Seated();
        string words = new('a', ChatService.MaximumLength + 1);

        // Act
        Task<ChatMessage> saying = this._chat.Say(id, first, ChatChannel.Team, words, this._cancellation);

        // Assert
        InvalidChatException refused = await Assert.ThrowsAsync<InvalidChatException>(() => saying);
        Assert.Equal(GameRequestFailure.Invalid, refused.Failure);
    }

    [Fact]
    public async Task Say_WhenTheGameIsUnknown_ThrowsGameNotFoundException()
    {
        // Arrange - nothing beyond the service

        // Act
        Task<ChatMessage> saying =
            this._chat.Say(new GameId("missing"), "token", ChatChannel.Table, "Hello", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<GameNotFoundException>(() => saying);
    }

    [Fact]
    public async Task Read_WhenAPlayerReads_GetsTheTableAndTheirOwnTeamOnly()
    {
        // Arrange
        (GameId id, string first, string second) = await this.Talked();

        // Act
        ImmutableList<ChatMessage> firstReads = await this._chat.Read(id, first, 0, this._cancellation);
        ImmutableList<ChatMessage> secondReads = await this._chat.Read(id, second, 0, this._cancellation);

        // Assert
        Assert.Equal(("1,2,3", "1,3,4"), (Numbers(firstReads), Numbers(secondReads)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("stranger")]
    public async Task Read_WhenNoSeatIsHeld_GetsTheTableOnly(string? token)
    {
        // Arrange
        (GameId id, _, _) = await this.Talked();

        // Act
        ImmutableList<ChatMessage> read = await this._chat.Read(id, token, 0, this._cancellation);

        // Assert
        Assert.Equal([1, 3], read.Select(message => message.Number));
    }

    [Fact]
    public async Task Read_WhenAfterAMessage_GetsOnlyWhatCameLater()
    {
        // Arrange
        (GameId id, _, string second) = await this.Talked();

        // Act
        ImmutableList<ChatMessage> read = await this._chat.Read(id, second, 3, this._cancellation);

        // Assert
        Assert.Equal([4], read.Select(message => message.Number));
    }

    private static string Numbers(ImmutableList<ChatMessage> said) =>
        string.Join(",", said.Select(message => message.Number));

    /// <summary>A game with a person at each seat, and their tokens.</summary>
    private async Task<(GameId Id, string First, string Second)> Seated()
    {
        GameId id = await this._games.Create(
            TinyRules.RulesetId,
            [SeatPlan.Open, SeatPlan.Open],
            this._cancellation
        );
        string first = await this._games.Claim(id, First, this._cancellation);
        string second = await this._games.Claim(id, Second, this._cancellation);
        return (id, first, second);
    }

    /// <summary>
    /// As <see cref="Seated"/>, once each has said something to the table and to their team: 1 and 2 by the first
    /// seat, 3 and 4 by the second.
    /// </summary>
    private async Task<(GameId Id, string First, string Second)> Talked()
    {
        (GameId id, string first, string second) = await this.Seated();
        _ = await this._chat.Say(id, first, ChatChannel.Table, "Hello", this._cancellation);
        _ = await this._chat.Say(id, first, ChatChannel.Team, "Mine", this._cancellation);
        _ = await this._chat.Say(id, second, ChatChannel.Table, "Hi", this._cancellation);
        _ = await this._chat.Say(id, second, ChatChannel.Team, "Ours", this._cancellation);
        return (id, first, second);
    }
}
