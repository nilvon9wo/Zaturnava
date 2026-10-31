using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Hosting.GitHub;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// The services a person may sign in with: each one known here whose client is set up in configuration, under
/// <c>SignIn:Providers:{name}</c> with its <c>ClientId</c> and <c>ClientSecret</c>; none, if none is. Whoever comes
/// back from one is held in a short-lived cookie (<see cref="ExternalScheme"/>), only until they are signed in as a
/// player. The claims they come back with are issued by the service's name.
/// </summary>
internal static class SignInServices
{
    public const string ProvidersKey = "SignIn:Providers";
    public const string ClientIdKey = "ClientId";
    public const string ClientSecretKey = "ClientSecret";
    public const string ExternalScheme = "External";

    private static readonly TimeSpan ExternalLifetime = TimeSpan.FromMinutes(10);

    private static readonly ImmutableList<ISignInProviding> Known = [new GitHubSignIn()];

    public static IServiceCollection AddSignIn(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSignIn(configuration, Known);

    public static IServiceCollection AddSignIn(
        this IServiceCollection services,
        IConfiguration configuration,
        ImmutableList<ISignInProviding> known
    )
    {
        ImmutableList<SignInClient> clients = [.. known.Select(provider => ClientOf(configuration, provider))
            .OfType<SignInClient>()];
        _ = clients.Aggregate(
            services.AddAuthentication().AddCookie(ExternalScheme, External),
            (added, client) => added.AddOAuth(client.Provider.Name, options => Configure(options, client))
        );
        return services.AddSingleton(new SignInProviders([.. clients.Select(client => client.Provider.Name)]));
    }

    /// <summary>Where the service sends someone back to, once they have agreed: <c>/signin-{name}</c>.</summary>
    public static string CallbackPath(ISignInProviding provider) =>
        "/signin-" + provider.Name.ToLowerInvariant();

    /// <summary>Asks the service who someone is, with the token just given for them, and takes their claims.</summary>
    public static async Task Identify(OAuthCreatingTicketContext context)
    {
        CancellationToken cancellationToken = context.HttpContext.RequestAborted;
        using HttpRequestMessage request = new(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Zaturnava", "1.0"));
        using HttpResponseMessage response = await context.Backchannel.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        context.RunClaimActions(await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken));
    }

    private static SignInClient? ClientOf(IConfiguration configuration, ISignInProviding provider)
    {
        IConfigurationSection section = configuration.GetSection($"{ProvidersKey}:{provider.Name}");
        return section[ClientIdKey] is { } id && section[ClientSecretKey] is { } secret
            ? new SignInClient(provider, id, secret)
            : null;
    }

    private static void External(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = "Zaturnava.External";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = ExternalLifetime;
    }

    private static void Configure(OAuthOptions options, SignInClient client)
    {
        options.SignInScheme = ExternalScheme;
        options.ClientId = client.Id;
        options.ClientSecret = client.Secret;
        options.CallbackPath = CallbackPath(client.Provider);
        options.AuthorizationEndpoint = client.Provider.Authorization.AbsoluteUri;
        options.TokenEndpoint = client.Provider.Token.AbsoluteUri;
        options.UserInformationEndpoint = client.Provider.UserInformation.AbsoluteUri;
        options.ClaimsIssuer = client.Provider.Name;
        foreach (string scope in client.Provider.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.ClaimActions.Add(new IdentityClaims(client.Provider));
        options.Events.OnCreatingTicket = Identify;
    }

    private sealed record SignInClient(ISignInProviding Provider, string Id, string Secret);
}

/// <summary>The names of the services a person may sign in with here.</summary>
internal sealed record SignInProviders(ImmutableList<string> Names);

/// <summary>
/// Who someone is, as the service says (<see cref="ISignInProviding.IdentityOf"/>): the key it knows them by, and
/// their name if they gave one.
/// </summary>
file sealed class IdentityClaims(ISignInProviding provider)
    : ClaimAction(ClaimTypes.NameIdentifier, ClaimValueTypes.String)
{
    private readonly ISignInProviding _provider = provider;

    public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
    {
        (string key, string? name) = this._provider.IdentityOf(userData);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, key, this.ValueType, issuer));
        if (name is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, name, this.ValueType, issuer));
        }
    }
}
