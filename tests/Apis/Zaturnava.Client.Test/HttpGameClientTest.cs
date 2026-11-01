using System.Collections.Immutable;
using System.Net;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Client.Test;

public sealed class HttpGameClientTest
{
    private const string HistoryJson =
        """
        {"format":1,"gameId":"g","rulesetId":"chaturaji-2027","seats":[],"actions":[],
         "outcome":{"winners":[0,2],"ending":"agreement","reason":"every player agreed to end it"}}
        """;

    private const string ViewJson =
        """
        {"id":"g","rulesetId":"chaturaji-2027","rulesetName":"Chaturaji 2027","version":1,"turnNumber":1,
         "decidingSeat":0,"cells":[],"pieces":[],"seats":[],"legalActions":[]}
        """;

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Catalog_WhenAsked_GetsTheCatalog()
    {
        // Arrange
        StubServer server = new(
            HttpStatusCode.OK,
            """
            {"rulesets":[{"id":"chaturaji-2027","name":"Chaturaji 2027","seatCount":4}],
            "bots":[{"id":"random","name":"Random","description":"Any move.","usesAi":false}]}
            """
        );

        // Act
        CatalogDto catalog = await Client(server).Catalog(this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/catalog", "chaturaji-2027", "random"),
            (server.Method, server.Path, catalog.Rulesets.Single().Id, catalog.Bots.Single().Id)
        );
    }

    [Fact]
    public async Task List_WhenAsked_GetsTheGames()
    {
        // Arrange
        StubServer server = new(
            HttpStatusCode.OK,
            """[{"id":"g","rulesetId":"chaturaji-2027","version":2,"openSeats":[1]}]"""
        );

        // Act
        ImmutableList<GameSummaryDto> games = await Client(server).List(this._cancellation);

        // Assert
        Assert.Equal(("GET", "/api/games", "g"), (server.Method, server.Path, games.Single().Id));
    }

    [Fact]
    public async Task Create_WhenGivenARequest_PostsItAsJson()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.Created, """{"gameId":"g"}""");

        // Act
        GameCreatedDto created =
            await Client(server).Create(new CreateGameRequest("chaturaji-2027", [null, "random"]), this._cancellation);

        // Assert
        Assert.Equal(
            (
                "POST",
                "/api/games",
                """{"rulesetId":"chaturaji-2027","bots":[null,"random"],"scenario":null,"continuing":null}""",
                "g"
            ),
            (server.Method, server.Path, server.Body, created.GameId)
        );
    }

    [Fact]
    public async Task Get_WhenGivenAGame_GetsItsView()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, ViewJson);

        // Act
        GameViewDto view = await Client(server).Get("g", this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/games/g", "Chaturaji 2027", null),
            (server.Method, server.Path, view.RulesetName, server.Authorization)
        );
    }

    [Fact]
    public async Task History_WhenGivenAGame_GetsItsHistory()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, HistoryJson);

        // Act
        GameHistoryDto history = await Client(server).History("g", this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/games/g/history", "agreement"),
            (server.Method, server.Path, history.Outcome?.Ending)
        );
    }

    [Fact]
    public async Task Histories_WhenAsked_GetsEveryFinishedGamesHistory()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, $"[{HistoryJson}]");

        // Act
        ImmutableList<GameHistoryDto> histories = await Client(server).Histories(this._cancellation);

        // Assert
        Assert.Equal(("GET", "/api/history", "g"), (server.Method, server.Path, histories.Single().GameId));
    }

    [Fact]
    public async Task FillOpenSeats_WhenAsked_PostsWithTheSeatsToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, ViewJson);

        // Act
        _ = await Client(server).FillOpenSeats("g", "secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games/g/fill", "Bearer secret"),
            (server.Method, server.Path, server.Authorization)
        );
    }

    [Fact]
    public async Task ReplaceAway_WhenAsked_PostsWithTheSeatsToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, ViewJson);

        // Act
        _ = await Client(server).ReplaceAway("g", "secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games/g/replace", "Bearer secret"),
            (server.Method, server.Path, server.Authorization)
        );
    }

    [Fact]
    public async Task Advance_WhenGivenAVersion_PostsIt()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, ViewJson);

        // Act
        _ = await Client(server).Advance("g", 7, this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games/g/advance", null, """{"version":7}"""),
            (server.Method, server.Path, server.Authorization, server.Body)
        );
    }

    [Fact]
    public async Task Claim_WhenGivenASeat_PostsToTheSeat()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"token":"secret"}""");

        // Act
        SeatClaimedDto claimed = await Client(server).Claim("g", 2, null, null, this._cancellation);

        // Assert
        Assert.Equal(("POST", "/api/games/g/seats/2", "secret"), (server.Method, server.Path, claimed.Token));
    }

    [Fact]
    public async Task Claim_WhenAPlayerOrANicknameIsGiven_SendsThePlayersTokenAndTheNickname()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"token":"secret"}""");

        // Act
        _ = await Client(server).Claim("g", 2, "player", new ClaimSeatRequest("Ada", true), this._cancellation);

        // Assert
        Assert.Equal(
            ("Bearer player", """{"nickname":"Ada","isAi":true}"""),
            (server.Authorization, server.Body)
        );
    }

    [Fact]
    public async Task Identify_WhenGivenASeatHeld_PutsWhoHoldsItToTheSeat()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.NoContent, string.Empty);

        // Act
        await Client(server).Identify("g", 2, "player", new IdentifySeatRequest("secret", "Ada"), this._cancellation);

        // Assert
        Assert.Equal(
            ("PUT", "/api/games/g/seats/2", "Bearer player", """{"token":"secret","nickname":"Ada"}"""),
            (server.Method, server.Path, server.Authorization, server.Body)
        );
    }

    [Fact]
    public async Task Identify_WhenRefused_ThrowsWithTheReason()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.Forbidden, """{"detail":"Not your seat."}""");

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            Client(server).Identify("g", 2, null, new IdentifySeatRequest("secret"), this._cancellation)
        );

        // Assert
        Assert.Equal((Refusal.Forbidden, "Not your seat."), (refused.Refusal, refused.Message));
    }

    [Fact]
    public async Task Submit_WhenGivenAnAction_PostsItWithTheSeatsToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, ViewJson);
        ActionDto pass = new("pass", ImmutableSortedDictionary.CreateRange([KeyValuePair.Create("actor", "0")]));

        // Act
        _ = await Client(server).Submit("g", "secret", pass, this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games/g/actions", "Bearer secret", """{"kind":"pass","arguments":{"actor":"0"}}"""),
            (server.Method, server.Path, server.Authorization, server.Body)
        );
    }

    [Fact]
    public async Task Chat_WhenGivenAToken_GetsWhatCameLaterWithIt()
    {
        // Arrange
        StubServer server = new(
            HttpStatusCode.OK,
            """[{"number":4,"seat":1,"channel":"team","text":"Hold","at":"2026-10-31T12:00:00+00:00"}]"""
        );

        // Act
        ImmutableList<ChatMessageDto> said = await Client(server).Chat("g", "secret", 3, this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/games/g/chat", "?after=3", "Bearer secret", "Hold"),
            (server.Method, server.Path, server.Query, server.Authorization, said.Single().Text)
        );
    }

    [Fact]
    public async Task Chat_WhenWatching_GetsWithoutAToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, "[]");

        // Act
        _ = await Client(server).Chat("g", null, 0, this._cancellation);

        // Assert
        Assert.Null(server.Authorization);
    }

    [Fact]
    public async Task Say_WhenGivenWords_PostsThemWithTheSeatsToken()
    {
        // Arrange
        StubServer server = new(
            HttpStatusCode.OK,
            """{"number":1,"seat":0,"channel":"table","text":"Hi","at":"2026-10-31T12:00:00+00:00"}"""
        );

        // Act
        ChatMessageDto said =
            await Client(server).Say("g", "secret", new SayRequest(ChatChannels.Table, "Hi"), this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games/g/chat", "Bearer secret", """{"channel":"table","text":"Hi"}""", 1),
            (server.Method, server.Path, server.Authorization, server.Body, said.Number)
        );
    }

    [Fact]
    public async Task SignInProviders_WhenAsked_GetsTheirNames()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """["GitHub"]""");

        // Act
        ImmutableList<string> providers = await Client(server).SignInProviders(this._cancellation);

        // Assert
        Assert.Equal(("GET", "/api/signin", "GitHub"), (server.Method, server.Path, providers.Single()));
    }

    [Fact]
    public async Task Me_WhenTheTokenStandsForAPlayer_GetsThemWithIt()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"id":"p","name":"Ada"}""");

        // Act
        PlayerDto? player = await Client(server).Me("secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/players/me", "Bearer secret", new PlayerDto("p", "Ada")),
            (server.Method, server.Path, server.Authorization, player)
        );
    }

    [Fact]
    public async Task Me_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.Unauthorized, string.Empty);

        // Act
        PlayerDto? player = await Client(server).Me("forgotten", this._cancellation);

        // Assert
        Assert.Null(player);
    }

    [Fact]
    public async Task Rename_WhenGivenAName_PutsItWithTheToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"id":"p","name":"Countess","services":["GitHub"]}""");

        // Act
        PlayerDto? player = await Client(server).Rename("secret", "Countess", this._cancellation);

        // Assert
        Assert.Equal(
            ("PUT", "/api/players/me", "Bearer secret", """{"name":"Countess"}""", "Countess", "GitHub"),
            (server.Method, server.Path, server.Authorization, server.Body, player?.Name, player?.Services?.Single())
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"code":"c0de"}""", "c0de")]
    [InlineData(HttpStatusCode.Unauthorized, "", null)]
    public async Task LinkCode_WhenAnswered_IsTheCodeIfTheTokenStandsForSomeone(
        HttpStatusCode status,
        string body,
        string? expected
    )
    {
        // Arrange
        StubServer server = new(status, body);

        // Act
        string? code = await Client(server).LinkCode("secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/players/me/link-code", "Bearer secret", expected),
            (server.Method, server.Path, server.Authorization, code)
        );
    }

    [Fact]
    public async Task Unlink_WhenGivenAService_DeletesItsLogin()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"id":"p","name":"Ada","services":["Google"]}""");

        // Act
        PlayerDto? player = await Client(server).Unlink("secret", "Git Hub", this._cancellation);

        // Assert
        Assert.Equal(
            ("DELETE", "/api/players/me/logins/Git%20Hub", "Bearer secret", "Google"),
            (server.Method, server.Path, server.Authorization, player?.Services?.Single())
        );
    }

    [Fact]
    public async Task SetLinks_WhenGivenLinks_PutsThemWithTheToken()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, """{"id":"p","name":"Ada","links":["https://a.example"]}""");

        // Act
        PlayerDto? player = await Client(server).SetLinks("secret", ["https://a.example"], this._cancellation);

        // Assert
        Assert.Equal(
            (
                "PUT",
                "/api/players/me/links",
                "Bearer secret",
                """{"links":["https://a.example"]}""",
                "https://a.example"
            ),
            (server.Method, server.Path, server.Authorization, server.Body, player?.Links?.Single())
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"token":"ai-1"}""", "ai-1")]
    [InlineData(HttpStatusCode.Unauthorized, "", null)]
    public async Task AiToken_WhenAnswered_IsTheTokenIfTheTokenStandsForSomeone(
        HttpStatusCode status,
        string body,
        string? expected
    )
    {
        // Arrange
        StubServer server = new(status, body);

        // Act
        string? token = await Client(server).AiToken("secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/players/me/ai-token", "Bearer secret", expected),
            (server.Method, server.Path, server.Authorization, token)
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"id":"p 1","name":"Ada","links":[]}""", "Ada")]
    [InlineData(HttpStatusCode.NotFound, "", null)]
    public async Task Profile_WhenAnswered_IsThePlayerIfKnown(HttpStatusCode status, string body, string? expected)
    {
        // Arrange
        StubServer server = new(status, body);

        // Act
        ProfileDto? profile = await Client(server).Profile("p 1", this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/players/p%201", null, expected),
            (server.Method, server.Path, server.Authorization, profile?.Name)
        );
    }

    [Fact]
    public async Task SignOut_WhenGivenAToken_DeletesItsSession()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.NoContent, string.Empty);

        // Act
        await Client(server).SignOut("secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("DELETE", "/api/players/me/session", "Bearer secret"),
            (server.Method, server.Path, server.Authorization)
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public async Task DeleteMe_WhenAnswered_SaysWhetherThereWasAnAccountToDelete(HttpStatusCode status, bool expected)
    {
        // Arrange
        StubServer server = new(status, string.Empty);

        // Act
        bool isDeleted = await Client(server).DeleteMe("secret", this._cancellation);

        // Assert
        Assert.Equal(
            ("DELETE", "/api/players/me", "Bearer secret", expected),
            (server.Method, server.Path, server.Authorization, isDeleted)
        );
    }

    [Fact]
    public async Task DeleteMe_WhenTheServerFails_ThrowsHttpRequestException()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.BadGateway, "<html>down</html>");

        // Act
        Task<bool> deleting = Client(server).DeleteMe("secret", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(() => deleting);
    }

    [Fact]
    public async Task Send_WhenTheServerRefusesWithAProblem_ThrowsItsRefusalAndDetail()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.Conflict, """{"title":"Conflict","detail":"Seat 2 is taken."}""");

        // Act
        Task<SeatClaimedDto> claiming = Client(server).Claim("g", 2, null, null, this._cancellation);
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() => claiming);

        // Assert
        Assert.Equal((Refusal.Conflict, "Seat 2 is taken."), (refused.Refusal, refused.Message));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task Send_WhenTheRefusalExplainsNothing_SaysItWasRefused(string body)
    {
        // Arrange
        StubServer server = new(HttpStatusCode.NotFound, body);

        // Act
        Task<GameViewDto> getting = Client(server).Get("g", this._cancellation);
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() => getting);

        // Assert
        Assert.Equal(
            (Refusal.NotFound, "The game server refused the request (404)."),
            (refused.Refusal, refused.Message)
        );
    }

    [Fact]
    public async Task Send_WhenTheServerFailsOtherwise_ThrowsHttpRequestException()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.BadGateway, "<html>down</html>");

        // Act
        Task<GameViewDto> getting = Client(server).Get("g", this._cancellation);
        HttpRequestException failed = await Assert.ThrowsAsync<HttpRequestException>(() => getting);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
    }

    [Fact]
    public async Task Send_WhenASuccessHasNoBody_ThrowsHttpRequestException()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.OK, "null");

        // Act
        Task<GameViewDto> getting = Client(server).Get("g", this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(() => getting);
    }

    private static HttpGameClient Client(StubServer server) => new(server.Client());
}
