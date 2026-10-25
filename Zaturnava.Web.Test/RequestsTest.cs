using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class RequestsTest
{
    [Fact]
    public async Task Attempt_WhenTheRequestSucceeds_HasNothingToTell()
    {
        // Arrange - nothing beyond the request

        // Act
        string? problem = await Requests.Attempt(() => Task.CompletedTask);

        // Assert
        Assert.Null(problem);
    }

    [Fact]
    public async Task Attempt_WhenTheServerRefuses_TellsWhy()
    {
        // Arrange
        GameRequestRefusedException refused = new(Refusal.Conflict, "Seat 2 is taken.");

        // Act
        string? problem = await Requests.Attempt(() => Task.FromException(refused));

        // Assert
        Assert.Equal("Seat 2 is taken.", problem);
    }

    [Fact]
    public async Task Attempt_WhenTheServerCannotBeReached_TellsWhy()
    {
        // Arrange
        HttpRequestException unreachable = new("No route to host.");

        // Act
        string? problem = await Requests.Attempt(() => Task.FromException(unreachable));

        // Assert
        Assert.Equal("No route to host.", problem);
    }

    [Fact]
    public async Task Attempt_WhenTheServerDoesNotAnswerInTime_TellsWhy()
    {
        // Arrange
        TaskCanceledException timedOut = new("The request timed out.");

        // Act
        string? problem = await Requests.Attempt(() => Task.FromException(timedOut));

        // Assert
        Assert.Equal("The request timed out.", problem);
    }

    [Fact]
    public async Task Attempt_WhenThePageItselfFails_LetsItFail()
    {
        // Arrange
        InvalidOperationException bug = new("A bug.");

        // Act
        Task<string?> attempting = Requests.Attempt(() => Task.FromException(bug));

        // Assert
        Assert.Same(bug, await Assert.ThrowsAsync<InvalidOperationException>(() => attempting));
    }
}
