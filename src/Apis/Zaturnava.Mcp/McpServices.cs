using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp;

/// <summary>
/// The MCP server's tools, and the game server they call: <c>ZATURNAVA_SERVER_URL</c> (an
/// environment variable, as MCP clients configure servers), or a server on this machine. A host that serves
/// MCP itself (the game server, over HTTP) takes the tools alone, and supplies what they call.
/// </summary>
public static class McpServices
{
    public const string ServerUrlKey = "ZATURNAVA_SERVER_URL";
    public const string LocalServer = "http://localhost:5164/";

    /// <summary>Adds the tools; the caller picks the transport (stdio, or a stream in tests).</summary>
    public static IMcpServerBuilder AddZaturnavaMcp(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSingleton(new HttpClient { BaseAddress = new Uri(configuration[ServerUrlKey] ?? LocalServer) })
            .AddSingleton<IGameServing, HttpGameClient>()
            .AddZaturnavaMcpTools();

    /// <summary>
    /// Adds the tools alone: the caller supplies the <see cref="IGameServing"/> they call, and picks the transport.
    /// </summary>
    public static IMcpServerBuilder AddZaturnavaMcpTools(this IServiceCollection services) =>
        services.AddMcpServer().WithTools<GameTools>();
}
