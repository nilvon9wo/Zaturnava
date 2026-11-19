using System.Net;
using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

public sealed class PageServingTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("zaturnava-page-").FullName;

    public PageServingTest()
    {
        File.WriteAllText(Path.Combine(this._folder, "index.html"), "<p>the page</p>");
        File.WriteAllText(Path.Combine(this._folder, "runtime.dat"), "bytes");
    }

    public void Dispose() => Directory.Delete(this._folder, recursive: true);

    [Fact]
    public async Task Get_WhenThePageIsAskedForAtItsPath_ServesItAndItsFiles()
    {
        // Arrange - the page served by the game server too (Stated 2026-11-19, OQ9)
        await using PagedServer server = new(this._folder);
        using HttpClient client = server.CreateClient();

        // Act
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        string page = await client.GetStringAsync(new Uri("play/?game=3fa2c9d1", UriKind.Relative), cancellation);
        string file = await client.GetStringAsync(new Uri("play/runtime.dat", UriKind.Relative), cancellation);

        // Assert
        Assert.Equal(("<p>the page</p>", "bytes"), (page, file));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_WhenNoFolderOrNoneThereIsNamed_ServesNoPage(bool isNamed)
    {
        // Arrange
        await using PagedServer server = new(isNamed ? Path.Combine(this._folder, "missing") : null);
        using HttpClient client = server.CreateClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync(
            new Uri("play/", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>The server, serving the page from <paramref name="folder"/>, if any.</summary>
    private sealed class PagedServer(string? folder) : ZaturnavaServer
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            _ = folder is null ? builder : builder.UseSetting(PageServing.FolderKey, folder);
        }
    }
}
