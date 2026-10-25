using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Hosting;
using Net.NowhereAtAll.Zaturnava.Hosting.Itch;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

public sealed class PageHostingTest
{
    [Fact]
    public void AddPageHosting_WhenNoProviderIsConfigured_AllowsNoPage()
    {
        // Arrange
        using ServiceProvider services = Services([]);

        // Act
        IPageHosting host = services.GetRequiredService<IPageHosting>();

        // Assert
        Assert.Equal((typeof(ListedPageHost), 0), (host.GetType(), host.Origins.Count));
    }

    [Fact]
    public void AddPageHosting_WhenOriginsAreListed_AllowsThem()
    {
        // Arrange
        using ServiceProvider services = Services(
            [
                (PageHosting.ProviderKey, PageHosting.Listed),
                (PageHosting.OriginsKey + ":Local", "http://localhost:5165"),
                (PageHosting.OriginsKey + ":Mirror", "https://mirror.example"),
            ]
        );

        // Act
        IPageHosting host = services.GetRequiredService<IPageHosting>();

        // Assert
        Assert.Equal(["http://localhost:5165", "https://mirror.example"], host.Origins);
    }

    [Fact]
    public void AddPageHosting_WhenTheProviderIsNamed_UsesItsAdapter()
    {
        // Arrange
        using ServiceProvider services = Services([(PageHosting.ProviderKey, PageHosting.Itch)]);

        // Act
        IPageHosting host = services.GetRequiredService<IPageHosting>();

        // Assert
        _ = Assert.IsType<ItchPageHost>(host);
    }

    [Fact]
    public void AddPageHosting_WhenTheProviderIsUnknown_ThrowsInvalidOperationException()
    {
        // Arrange
        IConfiguration configuration = Configuration([(PageHosting.ProviderKey, "Geocities")]);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddPageHosting(configuration)
        );

        // Assert
        Assert.Contains("'Geocities'", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider Services((string Key, string Value)[] settings) =>
        new ServiceCollection().AddPageHosting(Configuration(settings)).BuildServiceProvider();

    private static IConfiguration Configuration((string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value)))
            .Build();
}
