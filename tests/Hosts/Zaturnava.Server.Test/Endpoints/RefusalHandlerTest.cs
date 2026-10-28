using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class RefusalHandlerTest
{
    [Fact]
    public async Task TryHandleAsync_WhenTheExceptionIsNoRefusal_LeavesItToFail()
    {
        // Arrange
        await using ServiceProvider services =
            new ServiceCollection().AddOptions().AddProblemDetails().BuildServiceProvider();
        RefusalHandler handler = new(services.GetRequiredService<IProblemDetailsService>());

        // Act
        bool isHandled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException("A bug."),
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.False(isHandled);
    }
}
