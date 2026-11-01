using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Server.Composition;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// The server with GitHub set up to sign in with, but no round trip to it: whoever a request's
/// <see cref="VouchedHandler.Header"/> names, as <c>issuer|key|name</c>, is taken as having come back from it.
/// </summary>
public partial class SigningInServer : ZaturnavaServer
{
    public const string Page = "http://localhost:5165";

    /// <summary>Signs in whoever <paramref name="vouched"/> names (<c>issuer|key|name</c>): their token.</summary>
    public async Task<string> PlayerToken(string vouched, CancellationToken cancellationToken)
    {
        using HttpClient http = this.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "signin/done?origin=" + Uri.EscapeDataString(Page));
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.Header, vouched);
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        string page = await response.Content.ReadAsStringAsync(cancellationToken);
        return PostedToken().Match(page).Groups[1].Value;
    }

    [GeneratedRegex("\"token\":\"([^\"]+)\"")]
    private static partial Regex PostedToken();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting($"{SignInServices.ProvidersKey}:GitHub:{SignInServices.ClientIdKey}", "client")
            .UseSetting($"{SignInServices.ProvidersKey}:GitHub:{SignInServices.ClientSecretKey}", "secret")
            .ConfigureTestServices(services => services.Configure<AuthenticationOptions>(options =>
                options.SchemeMap[SignInServices.ExternalScheme].HandlerType = typeof(VouchedHandler)
            ));
    }
}

/// <summary>
/// Who has come back from a service, as a request's <see cref="Header"/> says: <c>issuer|key|name</c>, the name
/// left out if they gave none; a value with no key, someone whose service said nothing of who they are.
/// </summary>
internal sealed class VouchedHandler : IAuthenticationSignOutHandler
{
    public const string Header = "X-Vouched";

    /// <summary>
    /// The code to link with, that the sign-in carried through the service, if any: empty, if none, as a sign-in
    /// carried through a real service's cookie brings back no code.
    /// </summary>
    public const string LinkHeader = "X-Link";

    private HttpContext? _context;

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        this._context = context;
        return Task.CompletedTask;
    }

    public Task<AuthenticateResult> AuthenticateAsync() =>
        Task.FromResult(
            this._context?.Request.Headers[Header].ToString() is { Length: > 0 } vouched
                ? AuthenticateResult.Success(Ticket(vouched.Split('|'), this.Carried()))
                : AuthenticateResult.NoResult()
        );

    public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    public Task SignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    private AuthenticationProperties Carried() =>
        new(
            new Dictionary<string, string?>
            {
                [SignInEndpoints.LinkItem] = this._context?.Request.Headers[LinkHeader].ToString(),
            }
        );

    private static AuthenticationTicket Ticket(string[] parts, AuthenticationProperties properties)
    {
        ClaimsIdentity identity = new(SignInServices.ExternalScheme);
        identity.AddClaims(
            [
                .. parts.Skip(1).Take(1).Select(key => new Claim(ClaimTypes.NameIdentifier, key, null, parts[0])),
                .. parts.Skip(2).Select(name => new Claim(ClaimTypes.Name, name, null, parts[0])),
            ]
        );
        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, SignInServices.ExternalScheme);
    }
}
