using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp;

/// <summary>
/// The MCP server's tools, and the game server they call: <c>ZATURNAVA_SERVER_URL</c> (an
/// environment variable, as MCP clients configure servers), or a server on this machine.
/// </summary>
internal static class McpServices
{
    public const string ServerUrlKey = "ZATURNAVA_SERVER_URL";
    public const string LocalServer = "http://localhost:5164/";

    /// <summary>Adds the tools; the caller picks the transport (stdio, or a stream in tests).</summary>
    public static IMcpServerBuilder AddZaturnavaMcp(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSingleton(new HttpClient { BaseAddress = new Uri(configuration[ServerUrlKey] ?? LocalServer) })
            .AddSingleton<IGameServing, HttpGameClient>()
            .AddMcpServer()
            .WithTools<GameTools>();
}
