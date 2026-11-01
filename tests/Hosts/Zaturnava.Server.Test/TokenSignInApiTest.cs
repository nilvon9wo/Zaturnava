using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>Signing in with a service that hands its token back to the browser: itch.io.</summary>
public sealed class TokenSignInApiTest(ItchSigningInServer server) : IClassFixture<ItchSigningInServer>
{
    private const string Callback = "signin-itch";
    private const string Cookie = "Zaturnava.Itch";

    private readonly HttpClient _http =
        server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Providers_WhenItchIsSetUp_NameIt()
    {
        // Arrange - nothing beyond the server

        // Act
        List<string>? providers =
            await this._http.GetFromJsonAsync<List<string>>(GameRoutes.SignInProviders, this._cancellation);

        // Assert
        Assert.Equal(["Itch"], providers);
    }

    [Fact]
    public async Task SignIn_WhenThePageIsServed_SendsThePersonToItchForATokenKeepingTheState()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this._http.GetAsync(GameRoutes.SignIn("Itch", SigningInServer.Page), this._cancellation);

        // Assert
        string location = response.Headers.Location?.AbsoluteUri ?? string.Empty;
        string state = StateOf(location);
        Assert.Equal(
            (HttpStatusCode.Redirect, true, true, true, true, 32, true),
            (
                response.StatusCode,
                location.StartsWith("https://itch.io/user/oauth?client_id=itch-client&", StringComparison.Ordinal),
                location.Contains("scope=profile%3Ame&response_type=token&", StringComparison.Ordinal),
                location.Contains("redirect_uri=http%3A%2F%2Flocalhost%2Fsignin-itch&", StringComparison.Ordinal),
                CookieOf(response).StartsWith(Uri.EscapeDataString($"{state} "), StringComparison.Ordinal),
                state.Length,
                response.Headers.GetValues("Set-Cookie").Single().Contains("httponly", StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public async Task SignIn_WhenThePageIsNotServed_Refuses()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("Itch", "https://elsewhere.example"),
            this._cancellation
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Callback_WhenItchSendsThePersonBack_IsAPageThatPostsTheTokenOn()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(Callback, this._cancellation);

        // Assert
        string page = await response.Content.ReadAsStringAsync(this._cancellation);
        Assert.Equal(
            (HttpStatusCode.OK, "text/html", true),
            (
                response.StatusCode,
                response.Content.Headers.ContentType?.MediaType,
                page.Contains("access_token", StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public async Task Finish_WhenTheStateIsTheOneKept_SignsThePersonInAsItchSays()
    {
        // Arrange
        (string state, string cookie) = await this.Begin();

        // Act
        using HttpResponseMessage response = await this.Finish(cookie, state, ItchSigningInServer.Accepted);

        // Assert
        SignedIn? signedIn = await response.Content.ReadFromJsonAsync<SignedIn>(this._cancellation);
        PlayerDto? me = await new HttpGameClient(this._http).Me($"{signedIn?.Token}", this._cancellation);
        Assert.Equal(
            (HttpStatusCode.OK, "Leaf", SigningInServer.Page, "Leaf"),
            (response.StatusCode, signedIn?.Name, signedIn?.Origin, me?.Name)
        );
    }

    [Fact]
    public async Task SignIn_WhenGivenACodeToLinkWith_KeepsItWithTheState()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("Itch", SigningInServer.Page, "c0de"),
            this._cancellation
        );

        // Assert
        Assert.EndsWith(
            Uri.EscapeDataString($" {SigningInServer.Page} c0de"),
            CookieOf(response),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task SignIn_WhenTheCodeToLinkWithHasASpace_Refuses()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("Itch", SigningInServer.Page, "c0 de"),
            this._cancellation
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Finish_WhenLinkingTheLoginToItsOwnPlayer_SignsThemIn()
    {
        // Arrange
        (string state, string cookie) = await this.Begin();
        using HttpResponseMessage first = await this.Finish(cookie, state, ItchSigningInServer.Accepted);
        SignedIn? leaf = await first.Content.ReadFromJsonAsync<SignedIn>(this._cancellation);
        HttpGameClient client = new(this._http);
        string? code = await client.LinkCode($"{leaf?.Token}", this._cancellation);
        string kept = Uri.EscapeDataString($"{state} {SigningInServer.Page} {code}");

        // Act
        using HttpResponseMessage response = await this.Finish(kept, state, ItchSigningInServer.Accepted);

        // Assert
        SignedIn? linked = await response.Content.ReadFromJsonAsync<SignedIn>(this._cancellation);
        PlayerDto? me = await client.Me($"{linked?.Token}", this._cancellation);
        Assert.Equal(
            ((await client.Me($"{leaf?.Token}", this._cancellation))?.Id, "Itch"),
            (me?.Id, string.Join(',', me?.Services ?? []))
        );
    }

    [Fact]
    public async Task Finish_WhenTheCodeToLinkWithIsNoGood_RefusesSayingWhy()
    {
        // Arrange
        string kept = Uri.EscapeDataString($"state {SigningInServer.Page} bogus");

        // Act
        using HttpResponseMessage response = await this.Finish(kept, "state", ItchSigningInServer.Accepted);

        // Assert
        string body = await response.Content.ReadAsStringAsync(this._cancellation);
        Assert.Equal(
            (HttpStatusCode.Forbidden, true),
            (response.StatusCode, body.Contains("That link has run out", StringComparison.Ordinal))
        );
    }

    [Theory]
    [InlineData(null, "state", ItchSigningInServer.Accepted)]
    [InlineData("state", "state", ItchSigningInServer.Accepted)]
    [InlineData("state " + SigningInServer.Page + " a b", "state", ItchSigningInServer.Accepted)]
    [InlineData("state " + SigningInServer.Page, "other", ItchSigningInServer.Accepted)]
    [InlineData("state https://elsewhere.example", "state", ItchSigningInServer.Accepted)]
    [InlineData("state " + SigningInServer.Page, "state", "refused")]
    public async Task Finish_WhenTheStateIsNotTheOneKeptOrItchRefusesTheToken_Refuses(
        string? kept,
        string state,
        string token
    )
    {
        // Arrange
        string? cookie = kept is null ? null : Uri.EscapeDataString(kept);

        // Act
        using HttpResponseMessage response = await this.Finish(cookie, state, token);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string StateOf(string location) =>
        location[(location.IndexOf("state=", StringComparison.Ordinal) + "state=".Length)..];

    private static string CookieOf(HttpResponseMessage response)
    {
        string set = response.Headers.GetValues("Set-Cookie").Single();
        return set[(Cookie.Length + 1)..set.IndexOf(';', StringComparison.Ordinal)];
    }

    private async Task<(string State, string Cookie)> Begin()
    {
        using HttpResponseMessage response =
            await this._http.GetAsync(GameRoutes.SignIn("Itch", SigningInServer.Page), this._cancellation);
        return (StateOf($"{response.Headers.Location?.AbsoluteUri}"), CookieOf(response));
    }

    private async Task<HttpResponseMessage> Finish(string? cookie, string state, string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, Callback);
        request.Content = JsonContent.Create(new { accessToken = token, state });
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{Cookie}={cookie}");
        }

        return await this._http.SendAsync(request, this._cancellation);
    }

    private sealed record SignedIn(string Token, string Name, string Origin);
}

/// <summary>
/// The server with itch.io set up to sign in with, and itch.io's profile answered here: the token
/// <see cref="Accepted"/> stands for Leaf, number 1729; any other is refused.
/// </summary>
public sealed class ItchSigningInServer : ZaturnavaServer
{
    public const string Accepted = "itch-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting($"{SignInServices.ProvidersKey}:Itch:{SignInServices.ClientIdKey}", "itch-client")
            .ConfigureTestServices(services =>
                services
                    .AddHttpClient(TokenSignInClients.Backchannel)
                    .ConfigurePrimaryHttpMessageHandler(() => new ItchProfile())
            );
    }
}

/// <summary>itch.io's profile, for the token <see cref="ItchSigningInServer.Accepted"/> alone.</summary>
file sealed class ItchProfile : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new AuthenticationHeaderValue("Bearer", ItchSigningInServer.Accepted).Equals(request.Headers.Authorization)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"user":{"id":1729,"username":"leafo","display_name":"Leaf"}}""",
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
                : new HttpResponseMessage(HttpStatusCode.Unauthorized)
        );
}
