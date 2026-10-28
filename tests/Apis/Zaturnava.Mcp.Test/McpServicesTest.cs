using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Net.NowhereAtAll.Zaturnava.Mcp.Test;

public sealed class McpServicesTest
{
    [Theory]
    [InlineData(null, McpServices.LocalServer)]
    [InlineData("https://zaturnava.example/", "https://zaturnava.example/")]
    public void AddZaturnavaMcp_WhenTheServerUrlIsOrIsNotSet_CallsTheRightServer(string? configured, string expected)
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([KeyValuePair.Create(McpServices.ServerUrlKey, configured)])
            .Build();
        ServiceCollection services = new();
        _ = services.AddZaturnavaMcp(configuration);

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.Equal(expected, provider.GetRequiredService<HttpClient>().BaseAddress?.ToString());
    }
}
