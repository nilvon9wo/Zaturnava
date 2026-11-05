using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Mcp;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The MCP server hosted here, at <c>mcp</c> (streamable HTTP), for an AI whose client reaches a server by its
/// address rather than starting one: Zaturnava.Mcp's tools, calling this server's own HTTP API, as any other
/// client does, at <c>Mcp:SelfAddress</c>; or, if that is not configured (as in development), at the address the
/// request came to. A live server configures it, so that a request naming another host cannot have this server
/// call that host. Its calls carry the <see cref="SelfPass"/>, so that they count as the caller's.
/// Stateless: each request stands alone, so nothing is lost when the server restarts.
/// </summary>
internal static class McpEndpoints
{
    public const string Route = "mcp";

    /// <summary>The HTTP client by which the tools call this server.</summary>
    public const string SelfClient = "mcp-self";

    public const string SelfAddressKey = "Mcp:SelfAddress";

    public static IServiceCollection AddHostedMcp(this IServiceCollection services)
    {
        _ = services.AddHttpContextAccessor();
        _ = services.AddHttpClient(SelfClient);
        _ = services.AddZaturnavaMcpTools().WithHttpTransport(transport => transport.Stateless = true);
        _ = services.AddScoped<IGameServing>(provider => new HttpGameClient(SelfOf(provider)));
        return services;
    }

    public static IEndpointRouteBuilder MapHostedMcp(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapMcp(Route);
        return routes;
    }

    /// <summary>
    /// This server's own address, ending in "/": <c>Mcp:SelfAddress</c>, if configured (as a live server's is); else
    /// the address <paramref name="request"/> came to.
    /// </summary>
    public static string SelfAddressOf(IConfiguration configuration, HttpRequest request) =>
        configuration.GetValue(SelfAddressKey, $"{request.Scheme}://{request.Host}{request.PathBase}/");

    private static HttpClient SelfOf(IServiceProvider provider)
    {
        HttpContext context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        HttpRequest request = context.Request;
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(SelfClient);
        client.BaseAddress = new Uri(SelfAddressOf(provider.GetRequiredService<IConfiguration>(), request));
        client.DefaultRequestHeaders.Add(SelfPass.Header, provider.GetRequiredService<SelfPass>().Value);
        client.DefaultRequestHeaders.Add(SelfPass.ForHeader, SelfPass.OwnAddressOf(context));
        return client;
    }
}
