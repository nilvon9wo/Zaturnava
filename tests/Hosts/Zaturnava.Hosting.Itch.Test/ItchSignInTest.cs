using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Itch.Test;

public sealed class ItchSignInTest
{
    private readonly ItchSignIn _provider = new();

    [Fact]
    public void Endpoints_WhenSigningIn_AreItchsOwnAndAskOnlyForTheProfile()
    {
        // Arrange
        ItchSignIn provider = this._provider;

        // Act
        Uri[] endpoints = [provider.Authorization, provider.UserInformation];

        // Assert
        Assert.Equal(
            ("Itch", "https://itch.io/user/oauth", "https://api.itch.io/profile", "profile:me"),
            (provider.Name, endpoints[0].AbsoluteUri, endpoints[1].AbsoluteUri, string.Join(' ', provider.Scopes))
        );
    }

    [Theory]
    [InlineData("""{"user":{"id":1729,"username":"leafo","display_name":"Leaf"}}""", "Leaf")]
    [InlineData("""{"user":{"id":1729,"username":"leafo","display_name":null}}""", "leafo")]
    [InlineData("""{"user":{"id":1729,"username":"leafo"}}""", "leafo")]
    [InlineData("""{"user":{"id":1729}}""", null)]
    public void IdentityOf_WhenItchDescribesSomeone_IsTheirNumberAndTheirDisplayNameElseTheirUserName(
        string json,
        string? name
    )
    {
        // Arrange
        using JsonDocument user = JsonDocument.Parse(json);

        // Act
        (string Key, string? Name) identity = this._provider.IdentityOf(user.RootElement);

        // Assert
        Assert.Equal(("1729", name), identity);
    }
}
