using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>The MCP server hosted at <c>mcp</c>, as an AI's client reaches it over HTTP.</summary>
public sealed class McpApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly ZaturnavaServer _server = server;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListTools_WhenConnected_OffersTheGameTools()
    {
        // Arrange
        await using McpClient client = await this.Connected();

        // Act
        IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: this._cancellation);

        // Assert
        Assert.Contains("submit_action", tools.Select(tool => tool.Name));
    }

    [Fact]
    public async Task CallTool_WhenAskedForTheCatalog_AnswersWithThisServersCatalog()
    {
        // Arrange
        await using McpClient client = await this.Connected();

        // Act
        CallToolResult result = await client.CallToolAsync("catalog", cancellationToken: this._cancellation);

        // Assert
        string text = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
        Assert.Equal(
            (false, ZaturnavaServer.Chaturaji2027),
            (
                result.IsError == true,
                JsonDocument.Parse(text).RootElement.GetProperty("rulesets")[0].GetProperty("id").GetString()
            )
        );
    }

    private Task<McpClient> Connected() =>
        McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(this._server.Server.BaseAddress, "mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp,
                },
                this._server.CreateClient(),
                ownsHttpClient: true
            ),
            cancellationToken: this._cancellation
        );
}
