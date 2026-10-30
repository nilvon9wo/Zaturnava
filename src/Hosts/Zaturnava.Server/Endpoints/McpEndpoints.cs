using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Mcp;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// The MCP server hosted here, at <c>mcp</c> (streamable HTTP), for an AI whose client reaches a server by its
/// address rather than starting one: Zaturnava.Mcp's tools, calling this server's own HTTP API at the address
/// the request came to, as any other client does. Stateless: each request stands alone, so nothing is lost when
/// the server restarts.
/// </summary>
internal static class McpEndpoints
{
    public const string Route = "mcp";

    /// <summary>The HTTP client by which the tools call this server.</summary>
    public const string SelfClient = "mcp-self";

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

    private static HttpClient SelfOf(IServiceProvider provider)
    {
        HttpRequest request = provider.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request;
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(SelfClient);
        client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}/");
        return client;
    }
}
