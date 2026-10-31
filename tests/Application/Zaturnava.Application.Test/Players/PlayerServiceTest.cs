using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
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
    public async Task SignOut_WhenTheTokenWasIssued_LetsItGoAloneLeavingThePlayersOtherSessions()
    {
        // Arrange
        SignedIn here = await this._service.SignIn(Login, "Ada", this._cancellation);
        SignedIn elsewhere = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        await this._service.SignOut(here.Token, this._cancellation);

        // Assert
        Assert.Equal(
            (null, here.Player.Id),
            (
                await this._service.Find(here.Token, this._cancellation),
                (await this._service.Find(elsewhere.Token, this._cancellation))?.Id
            )
        );
    }

    [Fact]
    public async Task Delete_WhenTheTokenStandsForAPlayer_ForgetsThemAndEverySessionAndLogin()
    {
        // Arrange
        SignedIn here = await this._service.SignIn(Login, "Ada", this._cancellation);
        SignedIn elsewhere = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        bool isDeleted = await this._service.Delete(here.Token, this._cancellation);

        // Assert
        SignedIn again = await this._service.SignIn(Login, "Ada", this._cancellation);
        Assert.Equal(
            (true, null, false),
            (
                isDeleted,
                await this._service.Find(elsewhere.Token, this._cancellation),
                again.Player.Id == here.Player.Id
            )
        );
    }

    [Fact]
    public async Task Delete_WhenTheTokenStandsForNoOne_DeletesNothing()
    {
        // Arrange
        SignedIn signedIn = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        bool isDeleted = await this._service.Delete("not a token", this._cancellation);

        // Assert
        Assert.Equal(
            (false, signedIn.Player.Id),
            (isDeleted, (await this._service.Find(signedIn.Token, this._cancellation))?.Id)
        );
    }

    [Fact]
    public async Task NamesOf_WhenSeatsAreHeld_NamesEachPersonsByWhoClaimedIt()
    {
        // Arrange - Ada signed in; a guest; a player since deleted; a bot's seat and an open one, named by none
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        SignedIn gone = await this._service.SignIn(new Login("GitHub", "5678"), "Gone", this._cancellation);
        _ = await this._service.Delete(gone.Token, this._cancellation);
        HumanSeat adas = new("A", ada.Player.Id);
        HumanSeat guests = new("B");
        HumanSeat deleted = new("C", gone.Player.Id);
        SeatController[] seats = [adas, guests, deleted, new BotSeat("first"), new OpenSeat(), adas];

        // Act
        ImmutableDictionary<HumanSeat, string> names = await this._service.NamesOf(seats, this._cancellation);

        // Assert
        Assert.Equal(
            "A Ada|B guest|C deleted player",
            string.Join(
                '|',
                names.OrderBy(name => name.Key.TokenHash).Select(name => $"{name.Key.TokenHash} {name.Value}")
            )
        );
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
