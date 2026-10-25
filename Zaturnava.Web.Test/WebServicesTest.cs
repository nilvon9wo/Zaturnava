using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class WebServicesTest
{
    private const string Page = "https://games.example/zaturnava/";

    [Fact]
    public void AddZaturnavaWeb_WhenAServerIsConfigured_CallsIt()
    {
        // Arrange
        IConfiguration configuration = Configuration(
            (WebServices.ServerUrlKey, "https://zaturnava.example/"),
            (WebServices.PollSecondsKey, "5")
        );

        // Act
        using ServiceProvider services =
            new ServiceCollection().AddZaturnavaWeb(configuration, Page).BuildServiceProvider();

        // Assert
        Assert.Equal(
            ("https://zaturnava.example/", TimeSpan.FromSeconds(5), typeof(HttpGameClient)),
            (
                services.GetRequiredService<HttpClient>().BaseAddress?.ToString(),
                services.GetRequiredService<TableSettings>().PollInterval,
                services.GetRequiredService<IGameServing>().GetType()
            )
        );
    }

    [Fact]
    public void AddZaturnavaWeb_WhenNothingIsConfigured_CallsThePagesOwnSiteEveryTwoSeconds()
    {
        // Arrange
        IConfiguration configuration = Configuration();

        // Act
        using ServiceProvider services =
            new ServiceCollection().AddZaturnavaWeb(configuration, Page).BuildServiceProvider();

        // Assert
        Assert.Equal(
            (Page, TimeSpan.FromSeconds(2)),
            (
                services.GetRequiredService<HttpClient>().BaseAddress?.ToString(),
                services.GetRequiredService<TableSettings>().PollInterval
            )
        );
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value)))
            .Build();
}
