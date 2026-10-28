using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// What clients other than <see cref="Client.HttpGameClient"/> may send - other languages, careless
/// or hostile callers, browsers - and what HTTP itself sees: statuses, headers, CORS.
/// </summary>
public sealed class RawHttpTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly HttpClient _http = server.CreateClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_WhenTheGameIsCreated_AnswersCreatedWithItsLocation()
    {
        // Arrange
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);

        // Act
        using HttpResponseMessage response =
            await this._http.PostAsJsonAsync(GameRoutes.Games, request, this._cancellation);

        // Assert
        GameCreatedDto? created = await response.Content.ReadFromJsonAsync<GameCreatedDto>(this._cancellation);
        Assert.Equal(
            (HttpStatusCode.Created, "/" + GameRoutes.Game(created?.GameId ?? string.Empty)),
            (response.StatusCode, response.Headers.Location?.OriginalString)
        );
    }

    [Theory]
    [InlineData("""{"rulesetId":"chaturaji-2027"}""")]
    [InlineData("""{"bots":[null,null,null,null]}""")]
    [InlineData("""{"rulesetId":""")]
    public async Task Create_WhenTheRequestIsIncomplete_AnswersBadRequest(string json)
    {
        // Arrange
        using StringContent body = new(json, Encoding.UTF8, "application/json");

        // Act
        using HttpResponseMessage response = await this._http.PostAsync(GameRoutes.Games, body, this._cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Basic c2VjcmV0")]
    [InlineData("Bearer")]
    public async Task Submit_WhenNoSeatTokenIsSent_AnswersForbidden(string? authorization)
    {
        // Arrange
        string gameId = await this.CreateGame();
        using HttpRequestMessage request = new(HttpMethod.Post, GameRoutes.Actions(gameId));
        request.Content = JsonContent.Create(new { kind = "pass", arguments = new { actor = "0" } });
        _ = request.Headers.TryAddWithoutValidation("Authorization", authorization);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WhenTheActionHasNoArguments_AnswersBadRequest()
    {
        // Arrange
        string gameId = await this.CreateGame();
        using HttpResponseMessage claim =
            await this._http.PostAsync(GameRoutes.Seat(gameId, 0), null, this._cancellation);
        SeatClaimedDto? claimed = await claim.Content.ReadFromJsonAsync<SeatClaimedDto>(this._cancellation);
        using HttpRequestMessage request = new(HttpMethod.Post, GameRoutes.Actions(gameId));
        request.Content = JsonContent.Create(new { kind = "pass" });
        request.Headers.Authorization = new AuthenticationHeaderValue(GameRoutes.TokenScheme, claimed?.Token);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preflight_WhenTheOriginIsTheListedPage_AllowsIt()
    {
        // Arrange
        const string page = "http://localhost:5165";
        using HttpRequestMessage preflight = Preflight(page);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(preflight, this._cancellation);

        // Assert
        Assert.Equal([page], response.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_WhenTheOriginIsNotThePage_AllowsNothing()
    {
        // Arrange
        using HttpRequestMessage preflight = Preflight("https://elsewhere.example");

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(preflight, this._cancellation);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    /// <summary>What a browser asks before posting to the server from a page at <paramref name="origin"/>.</summary>
    private static HttpRequestMessage Preflight(string origin)
    {
        HttpRequestMessage preflight = new(HttpMethod.Options, GameRoutes.Games);
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        return preflight;
    }

    private async Task<string> CreateGame()
    {
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        using HttpResponseMessage response =
            await this._http.PostAsJsonAsync(GameRoutes.Games, request, this._cancellation);
        GameCreatedDto? created = await response.Content.ReadFromJsonAsync<GameCreatedDto>(this._cancellation);
        return created?.GameId ?? string.Empty;
    }
}
