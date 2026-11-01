using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>Signing in with a service, in a window of its own, and asking who a player's token stands for.</summary>
public sealed partial class SignInApiTest(SigningInServer server) : IClassFixture<SigningInServer>
{
    private readonly HttpClient _http =
        server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Providers_WhenOneIsSetUp_AreItsName()
    {
        // Arrange - nothing beyond the server

        // Act
        ImmutableList<string>? providers =
            await this._http.GetFromJsonAsync<ImmutableList<string>>(GameRoutes.SignInProviders, this._cancellation);

        // Assert
        Assert.Equal(["GitHub"], providers);
    }

    [Fact]
    public async Task SignIn_WhenTheServiceIsSetUpAndThePageServed_SendsThePersonToTheService()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("GitHub", SigningInServer.Page),
            this._cancellation
        );

        // Assert
        string location = response.Headers.Location?.AbsoluteUri ?? string.Empty;
        Assert.Equal(
            (HttpStatusCode.Redirect, true, true),
            (
                response.StatusCode,
                location.StartsWith("https://github.com/login/oauth/authorize?", StringComparison.Ordinal),
                location.Contains("signin-github", StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("Elsewhere", SigningInServer.Page, HttpStatusCode.NotFound)]
    [InlineData("GitHub", "https://elsewhere.example", HttpStatusCode.BadRequest)]
    public async Task SignIn_WhenTheServiceIsNotSetUpOrThePageNotServed_Refuses(
        string provider,
        string origin,
        HttpStatusCode refusal
    )
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this._http.GetAsync(GameRoutes.SignIn(provider, origin), this._cancellation);

        // Assert
        Assert.Equal(refusal, response.StatusCode);
    }

    [Theory]
    [InlineData("GitHub|1|Ada", "Ada")]
    [InlineData("GitHub|2", "Player")]
    public async Task Done_WhenTheServiceVouchedForSomeone_PostsTheirTokenToThePage(string vouched, string name)
    {
        // Arrange - nothing beyond the person vouched for

        // Act
        string page = await this.Done(vouched, SigningInServer.Page);

        // Assert
        PlayerDto? player = await this.Me(TokenOf(page));
        Assert.Equal(
            (name, true),
            (player?.Name, page.Contains("""}, "http://localhost:5165");""", StringComparison.Ordinal))
        );
    }

    [Fact]
    public async Task Done_WhenTheSameLoginComesBack_SignsInTheSamePlayer()
    {
        // Arrange
        PlayerDto? first = await this.Me(TokenOf(await this.Done("GitHub|3|Ada", SigningInServer.Page)));

        // Act
        PlayerDto? again = await this.Me(TokenOf(await this.Done("GitHub|3|Ada", SigningInServer.Page)));

        // Assert
        Assert.Equal(first, again);
    }

    [Theory]
    [InlineData(null, SigningInServer.Page, HttpStatusCode.Unauthorized)]
    [InlineData("GitHub", SigningInServer.Page, HttpStatusCode.Unauthorized)]
    [InlineData("GitHub|4|Ada", "https://elsewhere.example", HttpStatusCode.BadRequest)]
    public async Task Done_WhenNoOneWasVouchedForOrThePageIsNotServed_Refuses(
        string? vouched,
        string origin,
        HttpStatusCode refusal
    )
    {
        // Arrange
        using HttpRequestMessage request = DoneRequest(vouched, origin);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(refusal, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-token")]
    public async Task Me_WhenTheTokenStandsForNoOne_IsUnauthorized(string? token)
    {
        // Arrange
        using HttpRequestMessage request = new(HttpMethod.Get, GameRoutes.Me);
        request.Headers.Authorization =
            token is null ? null : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignOut_WhenTheTokenStandsForAPlayer_LetsItGoForGood()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|5|Ada", SigningInServer.Page));

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Delete, GameRoutes.Session, token);

        // Assert
        using HttpResponseMessage me = await this.Send(HttpMethod.Get, GameRoutes.Me, token);
        Assert.Equal((HttpStatusCode.NoContent, HttpStatusCode.Unauthorized), (response.StatusCode, me.StatusCode));
    }

    [Fact]
    public async Task Delete_WhenTheTokenStandsForAPlayer_ForgetsThemSoTheirLoginMakesANewOne()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|6|Ada", SigningInServer.Page));
        PlayerDto? before = await this.Me(token);

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Delete, GameRoutes.Me, token);

        // Assert
        PlayerDto? after = await this.Me(TokenOf(await this.Done("GitHub|6|Ada", SigningInServer.Page)));
        Assert.Equal((HttpStatusCode.NoContent, false), (response.StatusCode, before?.Id == after?.Id));
    }

    [Fact]
    public async Task Claim_WhenAPlayerSignedInTakesASeat_ShowsTheirNameThereUntilTheyAreDeleted()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|7|Ada", SigningInServer.Page));
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        _ = await client.Claim(created.GameId, 0, token, "Impostor", this._cancellation);

        // Assert
        string? named = (await client.Get(created.GameId, this._cancellation)).Seats[0].Player;
        using HttpResponseMessage deleted = await this.Send(HttpMethod.Delete, GameRoutes.Me, token);
        ImmutableList<GameSummaryDto> games = await client.List(this._cancellation);
        Assert.Equal(
            ("Ada", PlayerService.Deleted),
            (named, games.Single(game => game.Id == created.GameId).Players[0])
        );
    }

    [Fact]
    public async Task Claim_WhenThePlayerTokenStandsForNoOne_IsRefusedRatherThanSeatingAGuest()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Claim(created.GameId, 0, "signed-out", null, this._cancellation)
        );

        // Assert
        Assert.Equal(
            (Refusal.Forbidden, 1),
            (refused.Refusal, (await client.Get(created.GameId, this._cancellation)).Seats.Count(IsOpen))
        );

        static bool IsOpen(SeatDto seat) => seat.Controller == "open";
    }

    [Theory]
    [InlineData("api/players/me", null)]
    [InlineData("api/players/me", "not-a-token")]
    [InlineData("api/players/me/session", null)]
    public async Task Delete_WhenTheTokenIsMissingOrStandsForNoOne_IsUnauthorized(string route, string? token)
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Delete, route, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string route, string? token)
    {
        using HttpRequestMessage request = new(method, route);
        request.Headers.Authorization =
            token is null ? null : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        return await this._http.SendAsync(request, this._cancellation);
    }

    private static HttpRequestMessage DoneRequest(string? vouched, string origin)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "signin/done?origin=" + Uri.EscapeDataString(origin));
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.Header, vouched);
        return request;
    }

    private static string TokenOf(string page) => PostedToken().Match(page).Groups[1].Value;

    [GeneratedRegex("\"token\":\"([^\"]+)\"")]
    private static partial Regex PostedToken();

    private async Task<string> Done(string vouched, string origin)
    {
        using HttpRequestMessage request = DoneRequest(vouched, origin);
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);
        return await response.Content.ReadAsStringAsync(this._cancellation);
    }

    private async Task<PlayerDto?> Me(string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, GameRoutes.Me);
        request.Headers.Authorization = new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);
        return await response.Content.ReadFromJsonAsync<PlayerDto>(this._cancellation);
    }
}
