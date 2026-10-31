using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class PlayerServiceTest
{
    private static readonly Login Login = new("GitHub", "1234");

    private readonly PlayerService _service = new(new InMemoryPlayerStore());
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignIn_WhenTheLoginIsNew_MakesAPlayerNamedAsTheProviderNamesThem()
    {
        // Arrange - nothing beyond the empty store

        // Act
        SignedIn signedIn = await this._service.SignIn(Login, "  Ada  ", this._cancellation);

        // Assert
        Assert.Equal(("Ada", Login), (signedIn.Player.Name, signedIn.Player.Logins.Single()));
    }

    [Fact]
    public async Task SignIn_WhenTheLoginIsKnown_SignsInTheSamePlayerWithANewToken()
    {
        // Arrange
        SignedIn first = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        SignedIn again = await this._service.SignIn(Login, "Ada Lovelace", this._cancellation);

        // Assert
        Assert.Equal(
            (first.Player.Id, "Ada", false),
            (again.Player.Id, again.Player.Name, again.Token == first.Token)
        );
    }

    [Theory]
    [InlineData(null, PlayerService.Unnamed)]
    [InlineData("   ", PlayerService.Unnamed)]
    [InlineData("An extraordinarily long name that runs on  and on", "An extraordinarily long name that runs o")]
    public async Task SignIn_WhenTheProvidersNameIsMissingOrLong_NamesThePlayerWithinBounds(
        string? name,
        string expected
    )
    {
        // Arrange - nothing beyond the empty store

        // Act
        SignedIn signedIn = await this._service.SignIn(Login, name, this._cancellation);

        // Assert
        Assert.Equal(expected, signedIn.Player.Name);
    }

    [Fact]
    public async Task Find_WhenTheTokenWasIssued_IsItsPlayer()
    {
        // Arrange
        SignedIn signedIn = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        Player? found = await this._service.Find(signedIn.Token, this._cancellation);

        // Assert
        Assert.Equal(signedIn.Player.Id, found?.Id);
    }

    [Fact]
    public async Task Find_WhenTheTokenWasNeverIssued_IsNoOne()
    {
        // Arrange
        _ = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        Player? found = await this._service.Find("not a token", this._cancellation);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void New_WhenTwoAreMade_AreDifferent()
    {
        // Arrange - nothing

        // Act
        (PlayerId first, PlayerId second) = (PlayerId.New(), PlayerId.New());

        // Assert
        Assert.NotEqual(first, second);
    }
}
