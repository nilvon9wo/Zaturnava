using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

public sealed class AdvisorTest
{
    [Theory]
    [InlineData("https://advice.test/v1", "some-model", "key", true)]
    [InlineData("https://advice.test/v1/", "some-model", "key", true)]
    [InlineData(null, "some-model", "key", false)]
    [InlineData("https://advice.test/v1", "", "key", false)]
    [InlineData("https://advice.test/v1", "some-model", null, false)]
    [InlineData("", "some-model", "key", false)]
    [InlineData("https://advice.test/v1", null, "key", false)]
    [InlineData("https://advice.test/v1", "some-model", "", false)]
    public void AddZaturnava_WhenAnAdvisorIsConfigured_OffersPythiaOnlyIfItIsNamedInFull(
        string? url,
        string? model,
        string? key,
        bool isOffered
    )
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                [
                    KeyValuePair.Create("Advisor:BaseUrl", url),
                    KeyValuePair.Create("Advisor:Model", model),
                    KeyValuePair.Create("Advisor:ApiKey", key),
                ]
            )
            .Build();

        // Act
        IBotLocating bots = new ServiceCollection()
            .AddZaturnava(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IBotLocating>();

        // Assert
        Assert.Equal(isOffered, bots.Bots.Any(bot => bot is { Id: PythiaBot.BotId, UsesAi: true }));
    }
}
