using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Discord.Test;

public sealed class DiscordSignInTest
{
    private readonly DiscordSignIn _provider = new();

    [Fact]
    public void Endpoints_WhenSigningIn_AreDiscordsOwnAndAskOnlyWhoSomeoneIs()
    {
        // Arrange
        DiscordSignIn provider = this._provider;

        // Act
        Uri[] endpoints = [provider.Authorization, provider.Token, provider.UserInformation];

        // Assert
        Assert.Equal(
            (
                "Discord",
                "https://discord.com/oauth2/authorize",
                "https://discord.com/api/oauth2/token",
                "https://discord.com/api/users/@me",
                "identify"
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
    [InlineData("""{"id":"80351110224678912","username":"nelly","global_name":"Nelly"}""", "Nelly")]
    [InlineData("""{"id":"80351110224678912","username":"nelly","global_name":null}""", "nelly")]
    [InlineData("""{"id":"80351110224678912","username":"nelly"}""", "nelly")]
    [InlineData("""{"id":"80351110224678912"}""", null)]
    public void IdentityOf_WhenDiscordDescribesSomeone_IsTheirNumberAndTheirDisplayNameElseTheirUserName(
        string json,
        string? name
    )
    {
        // Arrange
        using JsonDocument user = JsonDocument.Parse(json);

        // Act
        (string Key, string? Name) identity = this._provider.IdentityOf(user.RootElement);

        // Assert
        Assert.Equal(("80351110224678912", name), identity);
    }
}
