using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// Signing in with a service that hands its token back to the browser (<see cref="ITokenSignInProviding"/>), in the
/// window the page opens, as with any other service (<see cref="SignInEndpoints"/>): <c>signin/{name}</c> sends the
/// person there, with a random state kept in a short-lived cookie; the service sends them back to
/// <c>signin-{name}</c>, a page that reads the token from the address's fragment and posts it, with the state, to the
/// same address; the server, if the state is the one it kept, asks the service who the person is, signs them in, and
/// answers with the player's token and the page's origin, to which the window posts it, and closes. The service's
/// token is used once, and not kept.
/// </summary>
internal static class TokenSignInEndpoints
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public static IEndpointRouteBuilder MapTokenSignIn(this IEndpointRouteBuilder routes)
    {
        foreach (TokenSignInClient client in routes.ServiceProvider.GetRequiredService<TokenSignInClients>().Clients)
        {
            string callback = SignInServices.CallbackPath(client.Provider.Name);
            _ = routes.MapGet(
                $"signin/{client.Provider.Name}",
                (string origin, HttpContext context, IPageHosting page) => Begin(client, origin, context, page)
            );
            _ = routes.MapGet(callback, () => TypedResults.Content(Receiving, "text/html"));
            _ = routes.MapPost(
                callback,
                (
                    TokenSignInRequest request,
                    HttpContext context,
                    IPageHosting page,
                    PlayerService players,
                    IHttpClientFactory http,
                    CancellationToken cancellationToken
                ) => Finish(client, request, context, page, players, http, cancellationToken)
            );
        }

        return routes;
    }

    /// <summary>The cookie that keeps the state and the page's origin, until the person comes back.</summary>
    public static string CookieOf(ITokenSignInProviding provider) => $"Zaturnava.{provider.Name}";

    private static Results<RedirectHttpResult, BadRequest> Begin(
        TokenSignInClient client,
        string origin,
        HttpContext context,
        IPageHosting page
    )
    {
        if (!page.Origins.Contains(origin))
        {
            return TypedResults.BadRequest();
        }

        HttpRequest request = context.Request;
        string state = RandomNumberGenerator.GetHexString(32);
        string callback = SignInServices.CallbackPath(client.Provider.Name);
        context.Response.Cookies.Append(
            CookieOf(client.Provider),
            $"{state} {origin}",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = Lifetime,
                Path = $"{request.PathBase}{callback}",
            }
        );
        string query = string.Join(
            "&",
            $"client_id={Uri.EscapeDataString(client.Id)}",
            $"scope={Uri.EscapeDataString(string.Join(' ', client.Provider.Scopes))}",
            "response_type=token",
            $"redirect_uri={Uri.EscapeDataString($"{request.Scheme}://{request.Host}{request.PathBase}{callback}")}",
            $"state={state}"
        );
        return TypedResults.Redirect($"{client.Provider.Authorization.AbsoluteUri}?{query}");
    }

    private static async Task<Results<Ok<TokenSignedIn>, UnauthorizedHttpResult>> Finish(
        TokenSignInClient client,
        TokenSignInRequest signingIn,
        HttpContext context,
        IPageHosting page,
        PlayerService players,
        IHttpClientFactory http,
        CancellationToken cancellationToken
    )
    {
        string cookie = CookieOf(client.Provider);
        if (context.Request.Cookies[cookie]?.Split(' ') is not [{ } state, { } origin]
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(state),
                Encoding.UTF8.GetBytes(signingIn.State)
            )
            || !page.Origins.Contains(origin))
        {
            return TypedResults.Unauthorized();
        }

        context.Response.Cookies.Delete(cookie);
        using HttpRequestMessage asking = new(HttpMethod.Get, client.Provider.UserInformation);
        asking.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signingIn.AccessToken);
        asking.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using HttpResponseMessage answer =
            await http.CreateClient(TokenSignInClients.Backchannel).SendAsync(asking, cancellationToken);
        if (!answer.IsSuccessStatusCode)
        {
            return TypedResults.Unauthorized();
        }

        (string key, string? name) =
            client.Provider.IdentityOf(await answer.Content.ReadFromJsonAsync<JsonElement>(cancellationToken));
        SignedIn signedIn = await players.SignIn(new Login(client.Provider.Name, key), name, cancellationToken);
        return TypedResults.Ok(new TokenSignedIn(signedIn.Token, signedIn.Player.Name, origin));
    }

    /// <summary>
    /// The page the service sends the person back to: it takes the token and the state from the address's fragment
    /// (and wipes them from the address), posts them to the server, and posts the player's token to the page that
    /// opened the window, at the origin the server names, and closes; or says that signing in did not work.
    /// </summary>
    private const string Receiving = """
        <!doctype html>
        <html lang="en">
        <meta charset="utf-8">
        <title>Signing in</title>
        <p id="said">Signing in…</p>
        <script>
        const fragment = new URLSearchParams(location.hash.slice(1));
        history.replaceState(null, "", location.pathname);
        fetch(location.pathname, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                accessToken: fragment.get("access_token") ?? "",
                state: fragment.get("state") ?? ""
            })
        })
            .then(response => response.ok ? response.json() : Promise.reject(response.status))
            .then(done => {
                window.opener?.postMessage({ token: done.token, name: done.name }, done.origin);
                window.close();
            })
            .catch(() => {
                document.getElementById("said").textContent =
                    "Signing in did not work: close this window, and try again.";
            });
        </script>
        </html>
        """;
}

/// <summary>What the page the service sent the person back to read from the address's fragment.</summary>
internal sealed record TokenSignInRequest(string AccessToken, string State);

/// <summary>The player signed in: their token and name, and the origin of the page to give them to.</summary>
internal sealed record TokenSignedIn(string Token, string Name, string Origin);
