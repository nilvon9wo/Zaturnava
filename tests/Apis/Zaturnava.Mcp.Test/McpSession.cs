using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp.Test;

/// <summary>
/// The MCP server, hosted in the test process with <see cref="Server"/> behind its tools, and an MCP
/// client talking to it over in-memory pipes: the protocol as an AI's client speaks it, minus stdio.
/// </summary>
internal sealed class McpSession : IAsyncDisposable
{
    private readonly IHost _host;

    private McpSession(IHost host, McpClient client, StubGameServer server)
    {
        this._host = host;
        this.Client = client;
        this.Server = server;
    }

    public StubGameServer Server { get; }

    public McpClient Client { get; }

    public static async Task<McpSession> Start(CancellationToken cancellationToken)
    {
        Pipe toServer = new();
        Pipe toClient = new();
        StubGameServer server = new();
        HostApplicationBuilder builder = new(new HostApplicationBuilderSettings { DisableDefaults = true });
        _ = builder.Services
            .AddZaturnavaMcp(builder.Configuration)
            .WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        _ = builder.Services.AddSingleton<IGameServing>(server);
        IHost host = builder.Build();
        await host.StartAsync(cancellationToken);
        McpClient client = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()),
            cancellationToken: cancellationToken
        );
        return new McpSession(host, client, server);
    }

    /// <summary>Calls a tool; its text reply, and whether it is an error.</summary>
    public async Task<(string Text, bool IsError)> Call(
        string tool,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        CallToolResult result = await this.Client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        string text = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
        return (text, result.IsError == true);
    }

    public async ValueTask DisposeAsync()
    {
        await this.Client.DisposeAsync();
        await this._host.StopAsync();
        this._host.Dispose();
    }
}
