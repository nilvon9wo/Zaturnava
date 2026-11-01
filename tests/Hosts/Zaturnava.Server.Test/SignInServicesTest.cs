using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

public sealed class SignInServicesTest
{
    private const string Name = "Fake";

    [Theory]
    [InlineData(null, null)]
    [InlineData("client", null)]
    [InlineData("", "secret")]
    [InlineData("client", "")]
    public void AddSignIn_WhenTheClientIsNotWhollySetUp_OffersNoService(string? id, string? secret)
    {
        // Arrange
        IConfiguration configuration = Configuration(id, secret);

        // Act
        using ServiceProvider services = Services(configuration);

        // Assert
        Assert.Empty(services.GetRequiredService<SignInProviders>().Names);
    }

    [Fact]
    public void AddSignIn_WhenTheClientIsSetUp_SendsPeopleToTheServiceAskingItsScopes()
    {
        // Arrange
        IConfiguration configuration = Configuration("client", "secret");

        // Act
        using ServiceProvider services = Services(configuration);

        // Assert
        OAuthOptions options = services.GetRequiredService<IOptionsMonitor<OAuthOptions>>().Get(Name);
        Assert.Equal(
            (
                Name,
                "client",
                "secret",
                "/signin-fake",
                "https://fake.example/authorize",
                "https://fake.example/token",
                "https://fake.example/user",
                "profile",
                SignInServices.ExternalScheme
            ),
            (
                string.Join(",", services.GetRequiredService<SignInProviders>().Names),
                options.ClientId,
                options.ClientSecret,
                options.CallbackPath.Value,
                options.AuthorizationEndpoint,
                options.TokenEndpoint,
                options.UserInformationEndpoint,
                string.Join(",", options.Scope),
                options.SignInScheme
            )
        );
    }

    [Fact]
    public void AddSignIn_WhenAsked_HoldsWhoeverComesBackInALaxCookieForTenMinutes()
    {
        // Arrange
        IConfiguration configuration = Configuration("client", "secret");

        // Act
        using ServiceProvider services = Services(configuration);

        // Assert
        CookieAuthenticationOptions options = services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(SignInServices.ExternalScheme);
        Assert.Equal(
            ("Zaturnava.External", SameSiteMode.Lax, TimeSpan.FromMinutes(10)),
            (options.Cookie.Name, options.Cookie.SameSite, options.ExpireTimeSpan)
        );
    }

    [Theory]
    [InlineData("""{"id":"7","name":"Ada"}""", "Ada")]
    [InlineData("""{"id":"7"}""", null)]
    public async Task Identify_WhenTheServiceSaysWhoSomeoneIs_ClaimsItAsTheService(string user, string? name)
    {
        // Arrange
        await using ServiceProvider services = Services(Configuration("client", "secret"));
        OAuthOptions options = services.GetRequiredService<IOptionsMonitor<OAuthOptions>>().Get(Name);
        using HttpClient backchannel = new(new Answering(user));
        using JsonDocument tokens = JsonDocument.Parse("""{"access_token":"t"}""");
        using JsonDocument nothing = JsonDocument.Parse("{}");
        ClaimsPrincipal principal = new(new ClaimsIdentity(Name));
        OAuthCreatingTicketContext context = new(
            principal,
            new AuthenticationProperties(),
            new DefaultHttpContext(),
            new AuthenticationScheme(Name, null, typeof(OAuthHandler<OAuthOptions>)),
            options,
            backchannel,
            OAuthTokenResponse.Success(tokens),
            nothing.RootElement
        );

        // Act
        await options.Events.CreatingTicket(context);

        // Assert
        Assert.Equal(
            ("7", Name, name),
            (
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Issuer,
                principal.FindFirst(ClaimTypes.Name)?.Value
            )
        );
    }

    private static IConfiguration Configuration(string? id, string? secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{SignInServices.ProvidersKey}:{Name}:{SignInServices.ClientIdKey}"] = id,
                    [$"{SignInServices.ProvidersKey}:{Name}:{SignInServices.ClientSecretKey}"] = secret,
                }
            )
            .Build();

    private static ServiceProvider Services(IConfiguration configuration) =>
        new ServiceCollection()
            .AddLogging()
            .AddSignIn(configuration, [new FakeSignIn()], [])
            .BuildServiceProvider();
}

/// <summary>A service to sign in with that asks leave to read a profile, and says who someone is.</summary>
file sealed class FakeSignIn : ISignInProviding
{
    public string Name => "Fake";

    public Uri Authorization { get; } = new("https://fake.example/authorize");

    public Uri Token { get; } = new("https://fake.example/token");

    public Uri UserInformation { get; } = new("https://fake.example/user");

    public ImmutableList<string> Scopes { get; } = ["profile"];

    public (string Key, string? Name) IdentityOf(JsonElement user) =>
        (
            user.GetProperty("id").GetString() ?? string.Empty,
            user.TryGetProperty("name", out JsonElement name) ? name.GetString() : null
        );
}

/// <summary>A service answering every request with the same user, as JSON.</summary>
file sealed class Answering(string user) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(user, Encoding.UTF8, "application/json"),
            }
        );
}
