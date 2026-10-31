using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.GitHub.Test;

public sealed class GitHubSignInTest
{
    private readonly GitHubSignIn _provider = new();

    [Fact]
    public void Endpoints_WhenSigningIn_AreGitHubsOwnAndAskNoLeave()
    {
        // Arrange
        GitHubSignIn provider = this._provider;

        // Act
        Uri[] endpoints = [provider.Authorization, provider.Token, provider.UserInformation];

        // Assert
        Assert.Equal(
            (
                "GitHub",
                "https://github.com/login/oauth/authorize",
                "https://github.com/login/oauth/access_token",
                "https://api.github.com/user",
                0
            ),
            (
                provider.Name,
                endpoints[0].AbsoluteUri,
                endpoints[1].AbsoluteUri,
                endpoints[2].AbsoluteUri,
                provider.Scopes.Count
            )
        );
    }

    [Theory]
    [InlineData("""{"id":583231,"login":"octocat","name":"The Octocat"}""", "The Octocat")]
    [InlineData("""{"id":583231,"login":"octocat","name":null}""", "octocat")]
    [InlineData("""{"id":583231,"login":"octocat"}""", "octocat")]
    [InlineData("""{"id":583231}""", null)]
    public void IdentityOf_WhenGitHubDescribesSomeone_IsTheirNumberAndTheirNameElseTheirLogin(
        string json,
        string? name
    )
    {
        // Arrange
        using JsonDocument user = JsonDocument.Parse(json);

        // Act
        (string Key, string? Name) identity = this._provider.IdentityOf(user.RootElement);

        // Assert
        Assert.Equal(("583231", name), identity);
    }
}
