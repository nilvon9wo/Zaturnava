using System.Net;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>What someone finds at the server's own address, and the API described as OpenAPI.</summary>
public sealed class IndexApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly HttpClient _client = server.CreateClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/")]
    [InlineData("/api")]
    public async Task Get_WhenAtTheServersAddressOrApi_SaysWhatItIsAndWhereToGo(string path)
    {
        // Arrange - nothing beyond the server

        // Act
        string body = await this._client.GetStringAsync(new Uri(path, UriKind.Relative), this._cancellation);

        // Assert
        JsonElement index = JsonDocument.Parse(body).RootElement;
        Assert.Equal(
            ("Zaturnava", "api/catalog", "openapi/v1.json"),
            (
                index.GetProperty("name").GetString(),
                index.GetProperty("catalog").GetString(),
                index.GetProperty("openApi").GetString()
            )
        );
    }

    [Fact]
    public async Task Get_WhenAskedForTheOpenApiDocument_DescribesTheRoutes()
    {
        // Arrange - nothing beyond the server

        // Act
        string body = await this._client.GetStringAsync(
            new Uri("/openapi/v1.json", UriKind.Relative),
            this._cancellation
        );

        // Assert
        JsonElement paths = JsonDocument.Parse(body).RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/catalog", out _), body);
    }

    [Fact]
    public async Task Get_WhenAskedForTheReference_ServesAPageToBrowseTheApi()
    {
        // Arrange - the page the index points to
        string body = await this._client.GetStringAsync(new Uri("/api", UriKind.Relative), this._cancellation);
        string reference = JsonDocument.Parse(body).RootElement.GetProperty("reference").GetString()!;

        // Act
        using HttpResponseMessage response = await this._client.GetAsync(
            new Uri("/" + reference, UriKind.Relative),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (HttpStatusCode.OK, "text/html"),
            (response.StatusCode, response.Content.Headers.ContentType?.MediaType)
        );
    }
}
