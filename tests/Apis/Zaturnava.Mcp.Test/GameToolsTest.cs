using System.Collections.Immutable;
using ModelContextProtocol.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp.Test;

/// <summary>The tools as an AI's MCP client sees them: listed, described, called.</summary>
public sealed class GameToolsTest : IAsyncLifetime
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;
    private McpSession _session = null!;

    public async ValueTask InitializeAsync() => this._session = await McpSession.Start(this._cancellation);

    public ValueTask DisposeAsync() => this._session.DisposeAsync();

    [Fact]
    public async Task ListTools_WhenAsked_OffersEveryRequestAClientCanMake()
    {
        // Arrange - nothing beyond the session

        // Act
        IList<McpClientTool> tools = await this._session.Client.ListToolsAsync(cancellationToken: this._cancellation);

        // Assert
        Assert.Equal(
            [
                "advance_game", "catalog", "claim_seat", "create_game", "game_history", "get_game", "list_games",
                "submit_action",
            ],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public async Task Catalog_WhenCalled_ListsTheRulesetsAndBots()
    {
        // Arrange - nothing beyond the session

        // Act
        (string text, bool isError) = await this._session.Call("catalog", Empty, this._cancellation);

        // Assert
        Assert.Equal(
            (false, true, true),
            (
                isError,
                text.Contains("\"chaturaji-2027\"", StringComparison.Ordinal),
                text.Contains("\"random\"", StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public async Task ListGames_WhenCalled_ListsTheGamesAndTheirOpenSeats()
    {
        // Arrange - nothing beyond the session

        // Act
        (string text, bool isError) = await this._session.Call("list_games", Empty, this._cancellation);

        // Assert
        Assert.Equal((false, true), (isError, text.Contains("\"openSeats\":[0]", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task CreateGame_WhenGivenSeats_CreatesTheGameWithThem()
    {
        // Arrange
        Dictionary<string, object?> arguments = new()
        {
            ["rulesetId"] = "chaturaji-2027",
            ["bots"] = new[] { null, "random" },
        };

        // Act
        (string text, _) = await this._session.Call("create_game", arguments, this._cancellation);

        // Assert
        CreateGameRequest? request = this._session.Server.Received as CreateGameRequest;
        Assert.Equivalent(
            (true, "chaturaji-2027", new[] { null, "random" }),
            (text.Contains(StubGameServer.GameId, StringComparison.Ordinal), request?.RulesetId, request?.Bots),
            strict: true
        );
    }

    [Fact]
    public async Task ClaimSeat_WhenGivenASeat_ReturnsItsToken()
    {
        // Arrange
        Dictionary<string, object?> arguments = new() { ["gameId"] = StubGameServer.GameId, ["seat"] = 2 };

        // Act
        (string text, _) = await this._session.Call("claim_seat", arguments, this._cancellation);

        // Assert
        Assert.Equal(
            (true, (StubGameServer.GameId, 2)),
            (text.Contains(StubGameServer.Token, StringComparison.Ordinal), this._session.Server.Received)
        );
    }

    [Fact]
    public async Task GetGame_WhenGivenAGame_ReturnsItsView()
    {
        // Arrange
        Dictionary<string, object?> arguments = new() { ["gameId"] = StubGameServer.GameId };

        // Act
        (string text, _) = await this._session.Call("get_game", arguments, this._cancellation);

        // Assert
        Assert.Equal(
            (true, StubGameServer.GameId),
            (text.Contains("\"decidingSeat\":0", StringComparison.Ordinal), this._session.Server.Received)
        );
    }

    [Fact]
    public async Task GameHistory_WhenGivenAGame_ReturnsHowItEnded()
    {
        // Arrange
        Dictionary<string, object?> arguments = new() { ["gameId"] = StubGameServer.GameId };

        // Act
        (string text, _) = await this._session.Call("game_history", arguments, this._cancellation);

        // Assert
        Assert.Equal(
            (true, StubGameServer.GameId),
            (text.Contains("\"ending\":\"agreement\"", StringComparison.Ordinal), this._session.Server.Received)
        );
    }

    [Fact]
    public async Task AdvanceGame_WhenGivenAVersion_AdvancesTheGameFromIt()
    {
        // Arrange
        Dictionary<string, object?> arguments = new() { ["gameId"] = StubGameServer.GameId, ["version"] = 3 };

        // Act
        _ = await this._session.Call("advance_game", arguments, this._cancellation);

        // Assert
        Assert.Equal((StubGameServer.GameId, 3), this._session.Server.Received);
    }

    [Fact]
    public async Task SubmitAction_WhenGivenAnAction_SubmitsItWithTheToken()
    {
        // Arrange
        Dictionary<string, object?> arguments = new()
        {
            ["gameId"] = StubGameServer.GameId,
            ["token"] = StubGameServer.Token,
            ["kind"] = "move",
            ["arguments"] = new Dictionary<string, string> { ["from"] = "8", ["to"] = "16", ["actor"] = "0" },
        };

        // Act
        _ = await this._session.Call("submit_action", arguments, this._cancellation);

        // Assert
        (string, string, ActionDto)? submitted = this._session.Server.Received as (string, string, ActionDto)?;
        Assert.Equivalent(
            (StubGameServer.GameId, StubGameServer.Token, "move", ImmutableSortedDictionary.CreateRange(
                [KeyValuePair.Create("actor", "0"), KeyValuePair.Create("from", "8"), KeyValuePair.Create("to", "16")]
            )),
            (submitted?.Item1, submitted?.Item2, submitted?.Item3.Kind, submitted?.Item3.Arguments),
            strict: true
        );
    }

    [Fact]
    public async Task Call_WhenTheServerRefuses_ReturnsTheReasonAsTheToolsError()
    {
        // Arrange
        this._session.Server.Failure = new GameRequestRefusedException(Refusal.Forbidden, "Not your seat.");

        // Act
        (string text, bool isError) = await this._session.Call("get_game", Game, this._cancellation);

        // Assert
        const string reason = "Refused (Forbidden): Not your seat.";
        Assert.Equal((true, true), (isError, text.EndsWith(reason, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Call_WhenTheServerCannotBeReached_SaysSoAsTheToolsError()
    {
        // Arrange
        this._session.Server.Failure = new HttpRequestException("Connection refused.");

        // Act
        (string text, bool isError) = await this._session.Call("get_game", Game, this._cancellation);

        // Assert
        const string reason = "The game server could not be reached: Connection refused.";
        Assert.Equal((true, true), (isError, text.EndsWith(reason, StringComparison.Ordinal)));
    }

    private static Dictionary<string, object?> Empty => [];

    private static Dictionary<string, object?> Game => new() { ["gameId"] = StubGameServer.GameId };
}
