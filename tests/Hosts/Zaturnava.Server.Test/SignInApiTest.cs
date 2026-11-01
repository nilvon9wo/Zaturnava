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
        Assert.Equal(
            (first?.Id, first?.Name, "GitHub"),
            (again?.Id, again?.Name, string.Join(',', again?.Services ?? []))
        );
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
        _ = await client.Claim(created.GameId, 0, token, new ClaimSeatRequest("Impostor"), this._cancellation);

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

    [Fact]
    public async Task Identify_WhenAGuestSignsInAfterTakingASeat_ShowsTheirNameThere()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        SeatClaimedDto seat = await client.Claim(created.GameId, 0, null, null, this._cancellation);
        string token = TokenOf(await this.Done("GitHub|8|Ada", SigningInServer.Page));

        // Act
        await client.Identify(created.GameId, 0, token, new IdentifySeatRequest(seat.Token), this._cancellation);

        // Assert
        SeatDto shown = (await client.Get(created.GameId, this._cancellation)).Seats[0];
        Assert.Equal(("Ada", false), (shown.Player, shown.IsGuest));
    }

    [Fact]
    public async Task Identify_WhenAGuestChoosesANicknameAfterTakingASeat_ShowsItThere()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        SeatClaimedDto seat = await client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act
        await client.Identify(
            created.GameId,
            0,
            null,
            new IdentifySeatRequest(seat.Token, "Hal"),
            this._cancellation
        );

        // Assert
        Assert.Equal("Hal (guest)", (await client.Get(created.GameId, this._cancellation)).Seats[0].Player);
    }

    [Fact]
    public async Task Identify_WhenTheTokenDoesNotHoldTheSeat_IsRefused()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        _ = await client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Identify(created.GameId, 0, null, new IdentifySeatRequest("not-its-token"), this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    [Theory]
    [InlineData("DELETE", "api/players/me", null)]
    [InlineData("DELETE", "api/players/me", "not-a-token")]
    [InlineData("DELETE", "api/players/me/session", null)]
    [InlineData("DELETE", "api/players/me/logins/GitHub", null)]
    [InlineData("DELETE", "api/players/me/logins/GitHub", "not-a-token")]
    [InlineData("POST", "api/players/me/link-code", null)]
    [InlineData("POST", "api/players/me/link-code", "not-a-token")]
    [InlineData("PUT", "api/players/me", null)]
    [InlineData("PUT", "api/players/me", "not-a-token")]
    public async Task Request_WhenTheTokenIsMissingOrStandsForNoOne_IsUnauthorized(
        string method,
        string route,
        string? token
    )
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this.Send(new HttpMethod(method), route, token, new RenameRequest("Ada"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rename_WhenTheTokenStandsForAPlayer_NamesThemAnew()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|9|Ada", SigningInServer.Page));

        // Act
        PlayerDto? renamed = await server.GameClient().Rename(token, "Countess", this._cancellation);

        // Assert
        Assert.Equal(("Countess", "Countess"), (renamed?.Name, (await this.Me(token))?.Name));
    }

    [Fact]
    public async Task SignIn_WhenGivenACodeToLinkWith_SendsThePersonToTheServiceStill()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("GitHub", SigningInServer.Page, "c0de"),
            this._cancellation
        );

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Done_WhenGivenACodeToLinkWith_LinksTheLoginToThePlayerWhoAskedForIt()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|10|Ada", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);

        // Act
        string page = await this.Done("Google|10|Ada G", SigningInServer.Page, code);

        // Assert
        PlayerDto? linked = await this.Me(TokenOf(page));
        PlayerDto? again = await this.Me(TokenOf(await this.Done("Google|10|Someone", SigningInServer.Page)));
        Assert.Equal(
            ("GitHub,Google", (await this.Me(token))?.Id, linked?.Id),
            (string.Join(',', linked?.Services ?? []), linked?.Id, again?.Id)
        );
    }

    [Fact]
    public async Task Done_WhenTheLoginToLinkIsAnotherPlayers_SaysSoAndSignsNoOneIn()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        _ = await this.Done("Google|11|Bea", SigningInServer.Page);
        string token = TokenOf(await this.Done("GitHub|11|Ada", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);

        // Act
        string page = await this.Done("Google|11|Bea", SigningInServer.Page, code);

        // Assert
        Assert.Equal(
            (true, string.Empty),
            (page.Contains("That Google account is already another player&#39;s here.", StringComparison.Ordinal),
                TokenOf(page))
        );
    }

    [Fact]
    public async Task Unlink_WhenThePlayerHasAnotherService_LetsItGoButNotTheLast()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|12|Ada", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);
        _ = await this.Done("Google|12|Ada", SigningInServer.Page, code);

        // Act
        PlayerDto? unlinked = await client.Unlink(token, "Google", this._cancellation);

        // Assert
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Unlink(token, "GitHub", this._cancellation)
        );
        Assert.Equal(("GitHub", Refusal.Conflict), (string.Join(',', unlinked?.Services ?? []), refused.Refusal));
    }

    private async Task<HttpResponseMessage> Send(
        HttpMethod method,
        string route,
        string? token,
        object? body = null
    )
    {
        using HttpRequestMessage request = new(method, route);
        request.Content = body is null ? null : JsonContent.Create(body);
        request.Headers.Authorization =
            token is null ? null : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        return await this._http.SendAsync(request, this._cancellation);
    }

    private static HttpRequestMessage DoneRequest(string? vouched, string origin, string? link = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "signin/done?origin=" + Uri.EscapeDataString(origin));
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.Header, vouched);
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.LinkHeader, link);
        return request;
    }

    private static string TokenOf(string page) => PostedToken().Match(page).Groups[1].Value;

    [GeneratedRegex("\"token\":\"([^\"]+)\"")]
    private static partial Regex PostedToken();

    private async Task<string> Done(string vouched, string origin, string? link = null)
    {
        using HttpRequestMessage request = DoneRequest(vouched, origin, link);
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
