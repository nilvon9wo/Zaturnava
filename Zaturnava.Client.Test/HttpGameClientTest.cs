using System.Collections.Immutable;
using System.Net;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Client.Test;

public sealed class HttpGameClientTest
{
    private const string ViewJson =
        """
        {"id":"g","rulesetId":"classic","rulesetName":"Classic","version":1,"turnNumber":1,
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
            {"rulesets":[{"id":"classic","name":"Classic","seatCount":4}],
            "bots":[{"id":"random","name":"Random","description":"Any move.","usesAi":false}]}
            """
        );

        // Act
        CatalogDto catalog = await Client(server).Catalog(this._cancellation);

        // Assert
        Assert.Equal(
            ("GET", "/api/catalog", "classic", "random"),
            (server.Method, server.Path, catalog.Rulesets.Single().Id, catalog.Bots.Single().Id)
        );
    }

    [Fact]
    public async Task List_WhenAsked_GetsTheGames()
    {
        // Arrange
        StubServer server = new(
            HttpStatusCode.OK,
            """[{"id":"g","rulesetId":"classic","version":2,"openSeats":[1]}]"""
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
            await Client(server).Create(new CreateGameRequest("classic", [null, "random"]), this._cancellation);

        // Assert
        Assert.Equal(
            ("POST", "/api/games", """{"rulesetId":"classic","bots":[null,"random"]}""", "g"),
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
            ("GET", "/api/games/g", "Classic", null),
            (server.Method, server.Path, view.RulesetName, server.Authorization)
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
        SeatClaimedDto claimed = await Client(server).Claim("g", 2, this._cancellation);

        // Assert
        Assert.Equal(("POST", "/api/games/g/seats/2", "secret"), (server.Method, server.Path, claimed.Token));
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
    public async Task Send_WhenTheServerRefusesWithAProblem_ThrowsItsRefusalAndDetail()
    {
        // Arrange
        StubServer server = new(HttpStatusCode.Conflict, """{"title":"Conflict","detail":"Seat 2 is taken."}""");

        // Act
        Task<SeatClaimedDto> claiming = Client(server).Claim("g", 2, this._cancellation);
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
