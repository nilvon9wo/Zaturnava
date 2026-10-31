using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// Signing in, in a window the page opens (<see cref="GameRoutes.SignIn"/>): the person is sent to the service,
/// comes back here, and is signed in as a player; the window then posts the player's token to the page, only if
/// the page's origin is one the server serves (<see cref="IPageHosting"/>), and closes.
/// </summary>
internal static class SignInEndpoints
{
    /// <summary>Where the window comes back to once the service has vouched for the person.</summary>
    public const string Done = "signin/done";

    public static IEndpointRouteBuilder MapSignIn(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet(GameRoutes.SignInProviders, (SignInProviders providers) => providers.Names);
        _ = routes.MapGet("signin/{provider}", Challenge);
        _ = routes.MapGet(Done, Finish);
        _ = routes.MapGet(GameRoutes.Me, Me);
        _ = routes.MapDelete(GameRoutes.Me, Delete);
        _ = routes.MapDelete(GameRoutes.Session, SignOut);
        return routes;
    }

    private static Results<ChallengeHttpResult, NotFound, BadRequest> Challenge(
        string provider,
        string origin,
        HttpRequest request,
        SignInProviders providers,
        IPageHosting page
    ) =>
        (providers.Names.Contains(provider), page.Origins.Contains(origin)) switch
        {
            (false, _) => TypedResults.NotFound(),
            (_, false) => TypedResults.BadRequest(),
            _ => TypedResults.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = $"{request.PathBase}/{Done}?origin={Uri.EscapeDataString(origin)}",
                },
                [provider]
            ),
        };

    private static async Task<Results<ContentHttpResult, BadRequest, UnauthorizedHttpResult>> Finish(
        string origin,
        HttpContext context,
        IPageHosting page,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        if (!page.Origins.Contains(origin))
        {
            return TypedResults.BadRequest();
        }

        AuthenticateResult external = await context.AuthenticateAsync(SignInServices.ExternalScheme);
        if (external.Principal?.FindFirst(ClaimTypes.NameIdentifier) is not { } identifier)
        {
            return TypedResults.Unauthorized();
        }

        await context.SignOutAsync(SignInServices.ExternalScheme);
        SignedIn signedIn = await players.SignIn(
            new Login(identifier.Issuer, identifier.Value),
            external.Principal.FindFirst(ClaimTypes.Name)?.Value,
            cancellationToken
        );
        SignedInDto posted = new(signedIn.Token, signedIn.Player.Name);
        return TypedResults.Content(Posting(posted, origin), "text/html");
    }

    private static async Task<Results<Ok<PlayerDto>, UnauthorizedHttpResult>> Me(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token && await players.Find(token, cancellationToken) is { } player
            ? TypedResults.Ok(new PlayerDto(player.Id.Value, player.Name))
            : TypedResults.Unauthorized();

    /// <summary>Forgets the player the token stands for, and everything that says who they were.</summary>
    private static async Task<Results<NoContent, UnauthorizedHttpResult>> Delete(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token && await players.Delete(token, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.Unauthorized();

    /// <summary>Lets the token's session go, whether or not it still stood for anyone.</summary>
    private static async Task<Results<NoContent, UnauthorizedHttpResult>> SignOut(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    )
    {
        if (SeatToken.OrNull(request) is not { } token)
        {
            return TypedResults.Unauthorized();
        }

        await players.SignOut(token, cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// A page that posts <paramref name="signedIn"/> to the window that opened it, if that window's page is at
    /// <paramref name="origin"/>, and closes. Both are written as JSON, which escapes anything that could end the
    /// script.
    /// </summary>
    private static string Posting(SignedInDto signedIn, string origin)
    {
        string name = WebUtility.HtmlEncode(signedIn.Name);
        string message = JsonSerializer.Serialize(signedIn, JsonSerializerOptions.Web);
        string target = JsonSerializer.Serialize(origin);
        return $$"""
            <!doctype html>
            <html lang="en">
            <meta charset="utf-8">
            <title>Signed in</title>
            <p>Signed in as {{name}}. This window should close by itself.</p>
            <script>
            window.opener?.postMessage({{message}}, {{target}});
            window.close();
            </script>
            </html>
            """;
    }
}
