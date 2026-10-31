using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Google.Test;

public sealed class GoogleSignInTest
{
    private readonly GoogleSignIn _provider = new();

    [Fact]
    public void Endpoints_WhenSigningIn_AreGooglesOwnAndAskOnlyWhoSomeoneIs()
    {
        // Arrange
        GoogleSignIn provider = this._provider;

        // Act
        Uri[] endpoints = [provider.Authorization, provider.Token, provider.UserInformation];

        // Assert
        Assert.Equal(
            (
                "Google",
                "https://accounts.google.com/o/oauth2/v2/auth",
                "https://oauth2.googleapis.com/token",
                "https://openidconnect.googleapis.com/v1/userinfo",
                "openid profile"
            ),
            (
                provider.Name,
                endpoints[0].AbsoluteUri,
                endpoints[1].AbsoluteUri,
                endpoints[2].AbsoluteUri,
                string.Join(' ', provider.Scopes)
            )
        );
    }

    [Theory]
    [InlineData("""{"sub":"110169484474386276334","name":"Ada Lovelace"}""", "Ada Lovelace")]
    [InlineData("""{"sub":"110169484474386276334","name":null}""", null)]
    [InlineData("""{"sub":"110169484474386276334"}""", null)]
    public void IdentityOf_WhenGoogleDescribesSomeone_IsTheirNumberAndTheirName(string json, string? name)
    {
        // Arrange
        using JsonDocument user = JsonDocument.Parse(json);

        // Act
        (string Key, string? Name) identity = this._provider.IdentityOf(user.RootElement);

        // Assert
        Assert.Equal(("110169484474386276334", name), identity);
    }
}
