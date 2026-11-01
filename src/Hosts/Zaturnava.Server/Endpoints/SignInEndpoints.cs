using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Games;
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

    /// <summary>What the sign-in carries through the service: the code to link it with, if it was given one.</summary>
    public const string LinkItem = "link";

    public static IEndpointRouteBuilder MapSignIn(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet(GameRoutes.SignInProviders, (SignInProviders providers) => providers.Names);
        _ = routes.MapGet("signin/{provider}", Challenge);
        _ = routes.MapGet(Done, Finish);
        _ = routes.MapGet(GameRoutes.Me, Me);
        _ = routes.MapPut(GameRoutes.Me, Rename);
        _ = routes.MapDelete(GameRoutes.Me, Delete);
        _ = routes.MapDelete(GameRoutes.Session, SignOut);
        _ = routes.MapPost(GameRoutes.LinkCode, LinkCode);
        _ = routes.MapDelete(GameRoutes.Logins + "/{provider}", Unlink);
        _ = routes.MapPut(GameRoutes.Links, SetLinks);
        _ = routes.MapPost(GameRoutes.AiToken, AiToken);
        _ = routes.MapGet(GameRoutes.Players + "/{playerId}", Profile);
        return routes;
    }

    /// <summary>
    /// The player as the API shows them: the services they may sign in with in the order linked, and their links.
    /// </summary>
    public static PlayerDto ToDto(Player player) =>
        new(
            player.Id.Value,
            player.Name,
            [.. player.Logins.Select(login => login.Provider).Distinct()],
            player.Links
        );

    /// <summary>
    /// The player whose login it is signed in; or, with a <paramref name="link"/> code, the login linked to the
    /// player the code was issued to, who is signed in. A sign-in carried through a service's cookie brings back an
    /// empty code, not none, if it was given none.
    /// </summary>
    public static Task<SignedIn> SignInOrLink(
        PlayerService players,
        Login login,
        string? name,
        string? link,
        CancellationToken cancellationToken
    ) =>
        string.IsNullOrEmpty(link)
            ? players.SignIn(login, name, cancellationToken)
            : players.Link(link, login, cancellationToken);

    private static Results<ChallengeHttpResult, NotFound, BadRequest> Challenge(
        string provider,
        string origin,
        string? link,
        HttpRequest request,
        SignInProviders providers,
        IPageHosting page
    ) =>
        (providers.Names.Contains(provider), page.Origins.Contains(origin)) switch
        {
            (false, _) => TypedResults.NotFound(),
            (_, false) => TypedResults.BadRequest(),
            _ => TypedResults.Challenge(
                new AuthenticationProperties(new Dictionary<string, string?> { [LinkItem] = link })
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
        if (external.Ticket is not { } ticket
            || ticket.Principal.FindFirst(ClaimTypes.NameIdentifier) is not { } identifier)
        {
            return TypedResults.Unauthorized();
        }

        await context.SignOutAsync(SignInServices.ExternalScheme);
        try
        {
            SignedIn signedIn = await SignInOrLink(
                players,
                new Login(identifier.Issuer, identifier.Value),
                ticket.Principal.FindFirst(ClaimTypes.Name)?.Value,
                ticket.Properties.GetString(LinkItem),
                cancellationToken
            );
            SignedInDto posted = new(signedIn.Token, signedIn.Player.Name);
            return TypedResults.Content(Posting(posted, origin), "text/html");
        }
        catch (GameRequestException refused)
        {
            return TypedResults.Content(Refused(refused.Message), "text/html");
        }
    }

    private static async Task<Results<Ok<PlayerDto>, UnauthorizedHttpResult>> Me(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token && await players.Find(token, cancellationToken) is { } player
            ? TypedResults.Ok(ToDto(player))
            : TypedResults.Unauthorized();

    private static async Task<Results<Ok<PlayerDto>, UnauthorizedHttpResult>> Rename(
        RenameRequest renaming,
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token
        && await players.Rename(token, renaming.Name, cancellationToken) is { } player
            ? TypedResults.Ok(ToDto(player))
            : TypedResults.Unauthorized();

    private static async Task<Results<Ok<LinkCodeDto>, UnauthorizedHttpResult>> LinkCode(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token && await players.LinkCode(token, cancellationToken) is { } code
            ? TypedResults.Ok(new LinkCodeDto(code))
            : TypedResults.Unauthorized();

    private static async Task<Results<Ok<PlayerDto>, UnauthorizedHttpResult>> Unlink(
        string provider,
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token
        && await players.Unlink(token, provider, cancellationToken) is { } player
            ? TypedResults.Ok(ToDto(player))
            : TypedResults.Unauthorized();

    private static async Task<Results<Ok<PlayerDto>, UnauthorizedHttpResult>> SetLinks(
        LinksRequest links,
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token
        && await players.SetLinks(token, links.Links, cancellationToken) is { } player
            ? TypedResults.Ok(ToDto(player))
            : TypedResults.Unauthorized();

    private static async Task<Results<Ok<AiTokenDto>, UnauthorizedHttpResult>> AiToken(
        HttpRequest request,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        SeatToken.OrNull(request) is { } token && await players.AiToken(token, cancellationToken) is { } issued
            ? TypedResults.Ok(new AiTokenDto(issued))
            : TypedResults.Unauthorized();

    /// <summary>The player as anyone may see them: their name and links.</summary>
    private static async Task<Results<Ok<ProfileDto>, NotFound>> Profile(
        string playerId,
        PlayerService players,
        CancellationToken cancellationToken
    ) =>
        await players.Profile(new PlayerId(playerId), cancellationToken) is { } player
            ? TypedResults.Ok(new ProfileDto(player.Id.Value, player.Name, player.Links))
            : TypedResults.NotFound();

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

    /// <summary>A page that says why signing in was refused, and leaves the window open to be read.</summary>
    private static string Refused(string why) =>
        $$"""
            <!doctype html>
            <html lang="en">
            <meta charset="utf-8">
            <title>Not signed in</title>
            <p>{{WebUtility.HtmlEncode(why)}} Close this window to go back.</p>
            </html>
            """;
}
