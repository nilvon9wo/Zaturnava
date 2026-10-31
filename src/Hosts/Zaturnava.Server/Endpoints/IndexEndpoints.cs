using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// What someone finds at the server's own address, or at <c>api</c>: what it is, and where to go from there - the
/// catalog, the games, every finished game's history, the API described as OpenAPI and as a page to browse and
/// try it on, the MCP server for AIs, how to use it, and what is kept about the people who play.
/// </summary>
internal static class IndexEndpoints
{
    public const string OpenApiDocument = "openapi/v1.json";

    public const string ReferencePrefix = "/reference";

    public const string Reference = "reference/v1";

    public const string Documentation =
        "https://github.com/nilvon9wo/Zaturnava/blob/main/docs/bots/http-api.md";

    /// <summary>What is kept about the people who play: the server sends them on to the statement.</summary>
    public const string Privacy = "privacy";

    public const string PrivacyStatement = "https://github.com/nilvon9wo/Zaturnava/blob/main/docs/privacy.md";

    private static readonly ServerIndex Index = new(
        "Zaturnava",
        "A game server for Chaturaji 2027, a four-player game of chaturaji: JSON over HTTP, for people and bots.",
        GameRoutes.Catalog,
        GameRoutes.Games,
        GameRoutes.Histories,
        OpenApiDocument,
        Reference,
        McpEndpoints.Route,
        Documentation,
        Privacy
    );

    public static IEndpointRouteBuilder MapIndex(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGet("/", () => Index);
        _ = routes.MapGet("api", () => Index);
        _ = routes.MapGet(Privacy, () => Results.Redirect(PrivacyStatement));
        return routes;
    }
}

/// <summary>The server's index: its name, what it is, and its routes, relative to its own address.</summary>
internal sealed record ServerIndex(
    string Name,
    string Description,
    string Catalog,
    string Games,
    string Histories,
    string OpenApi,
    string Reference,
    string Mcp,
    string Documentation,
    string Privacy
);
