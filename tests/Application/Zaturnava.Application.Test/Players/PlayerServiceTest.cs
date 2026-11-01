using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class PlayerServiceTest
{
    private static readonly Login Login = new("GitHub", "1234");
    private static readonly Login Other = new("Google", "abcd");

    private readonly SteppedTime _time = new();
    private readonly PlayerService _service;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public PlayerServiceTest() => this._service = new(new InMemoryPlayerStore(), new LinkCodes(this._time));

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
        HumanSeat nicknamed = new("D", Nickname: "Bea");
        HumanSeat adasAi = new("E", ada.Player.Id, IsAi: true);
        HumanSeat aiGuest = new("F", IsAi: true);
        HumanSeat namedAi = new("G", Nickname: "Hal", IsAi: true);
        SeatController[] seats =
        [
            adas, guests, deleted, nicknamed, adasAi, aiGuest, namedAi, new BotSeat("first"), new OpenSeat(), adas,
        ];

        // Act
        ImmutableDictionary<HumanSeat, string> names = await this._service.NamesOf(seats, this._cancellation);

        // Assert
        Assert.Equal(
            "A Ada|B guest|C deleted player|D Bea (guest)|E Ada (AI)|F AI guest|G Hal (AI guest)",
            string.Join(
                '|',
                names.OrderBy(name => name.Key.TokenHash).Select(name => $"{name.Key.TokenHash} {name.Value}")
            )
        );
    }

    [Fact]
    public async Task Rename_WhenGivenAName_NamesThePlayerSoFromNowOn()
    {
        // Arrange
        SignedIn signedIn = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        Player? renamed = await this._service.Rename(signedIn.Token, " Countess\u0007 Lovelace ", this._cancellation);

        // Assert
        Assert.Equal(
            ("Countess Lovelace", "Countess Lovelace"),
            (renamed?.Name, (await this._service.Find(signedIn.Token, this._cancellation))?.Name)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  \t ")]
    public async Task Rename_WhenNothingIsLeftOfTheName_LeavesTheirs(string? name)
    {
        // Arrange
        SignedIn signedIn = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        Player? renamed = await this._service.Rename(signedIn.Token, name, this._cancellation);

        // Assert
        Assert.Equal(
            ("Ada", "Ada"),
            (renamed?.Name, (await this._service.Find(signedIn.Token, this._cancellation))?.Name)
        );
    }

    [Fact]
    public async Task Rename_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange - nothing beyond the empty store

        // Act
        Player? renamed = await this._service.Rename("not a token", "Ada", this._cancellation);

        // Assert
        Assert.Null(renamed);
    }

    [Fact]
    public async Task LinkCode_WhenTheTokenStandsForNoOne_IsNone()
    {
        // Arrange - nothing beyond the empty store

        // Act
        string? code = await this._service.LinkCode("not a token", this._cancellation);

        // Assert
        Assert.Null(code);
    }

    [Fact]
    public async Task Link_WhenTheLoginIsNoOnes_MakesItThePlayersSoEitherSignsThemIn()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string code = await this.CodeFor(ada);

        // Act
        SignedIn linked = await this._service.Link(code, Other, this._cancellation);

        // Assert
        SignedIn again = await this._service.SignIn(Other, "Someone else", this._cancellation);
        Assert.Equal(
            (ada.Player.Id, ada.Player.Id, 2, false),
            (linked.Player.Id, again.Player.Id, again.Player.Logins.Count, linked.Token == ada.Token)
        );
    }

    [Fact]
    public async Task Link_WhenTheLoginIsTheirsAlready_SignsThemIn()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string code = await this.CodeFor(ada);

        // Act
        SignedIn linked = await this._service.Link(code, Login, this._cancellation);

        // Assert
        Assert.Equal((ada.Player.Id, 1), (linked.Player.Id, linked.Player.Logins.Count));
    }

    [Fact]
    public async Task Link_WhenTheLoginIsAnotherPlayers_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        _ = await this._service.SignIn(Other, "Bea", this._cancellation);
        string code = await this.CodeFor(ada);

        // Act
        LoginTakenException refused = await Assert.ThrowsAsync<LoginTakenException>(() =>
            this._service.Link(code, Other, this._cancellation)
        );

        // Assert
        Assert.Equal("That Google account is already another player's here.", refused.Message);
    }

    [Fact]
    public async Task Link_WhenTheCodeWasUsedAlready_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string code = await this.CodeFor(ada);
        _ = await this._service.Link(code, Login, this._cancellation);

        // Act
        Task linking = this._service.Link(code, Other, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<LinkExpiredException>(() => linking);
    }

    [Fact]
    public async Task Link_WhenTheCodesTimeIsUp_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string code = await this.CodeFor(ada);
        this._time.Pass(LinkCodes.Lifetime);

        // Act
        Task linking = this._service.Link(code, Other, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<LinkExpiredException>(() => linking);
    }

    [Fact]
    public async Task Link_WhenAnEarlierCodeRanOutBeforeThisWasIssued_Links()
    {
        // Arrange - the code run out is let go as the new one is issued; that one is kept as a third is
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        _ = await this.CodeFor(ada);
        this._time.Pass(LinkCodes.Lifetime);
        string code = await this.CodeFor(ada);
        _ = await this.CodeFor(ada);

        // Act
        SignedIn linked = await this._service.Link(code, Other, this._cancellation);

        // Assert
        Assert.Equal(ada.Player.Id, linked.Player.Id);
    }

    [Fact]
    public async Task Link_WhenThePlayerIsGone_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string code = await this.CodeFor(ada);
        _ = await this._service.Delete(ada.Token, this._cancellation);

        // Act
        Task linking = this._service.Link(code, Other, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<LinkExpiredException>(() => linking);
    }

    [Fact]
    public async Task Unlink_WhenThePlayerHasAnotherLogin_LetsThatServicesGo()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        _ = await this._service.Link(await this.CodeFor(ada), Other, this._cancellation);

        // Act
        Player? unlinked = await this._service.Unlink(ada.Token, Other.Provider, this._cancellation);

        // Assert
        SignedIn again = await this._service.SignIn(Other, "Bea", this._cancellation);
        Assert.Equal(
            (Login, false),
            (unlinked?.Logins.Single(), again.Player.Id == ada.Player.Id)
        );
    }

    [Fact]
    public async Task Unlink_WhenItIsTheirLastLogin_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        LastLoginException refused = await Assert.ThrowsAsync<LastLoginException>(() =>
            this._service.Unlink(ada.Token, Login.Provider, this._cancellation)
        );

        // Assert
        Assert.Equal("GitHub is the only way left to sign in as this player: link another first.", refused.Message);
    }

    [Fact]
    public async Task Unlink_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange - nothing beyond the empty store

        // Act
        Player? unlinked = await this._service.Unlink("not a token", Login.Provider, this._cancellation);

        // Assert
        Assert.Null(unlinked);
    }

    [Fact]
    public async Task SetLinks_WhenTheyAreWebAddresses_ShowsThemTrimmedWithoutBlanksOrRepeatsAtMostFive()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string?[] links =
        [
            " https://a.example ", null, "  ", "https://a.example", "http://b.example", "https://c.example",
            "https://d.example", "https://e.example", "https://f.example",
        ];

        // Act
        Player? shown = await this._service.SetLinks(ada.Token, links, this._cancellation);

        // Assert
        const string expected =
            "https://a.example http://b.example https://c.example https://d.example https://e.example";
        Assert.Equal(
            (expected, expected),
            (
                string.Join(' ', shown?.Links ?? []),
                string.Join(' ', (await this._service.Profile(ada.Player.Id, this._cancellation))?.Links ?? [])
            )
        );
    }

    [Theory]
    [InlineData("ftp://a.example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("a.example")]
    public async Task SetLinks_WhenOneIsNoWebAddress_IsRefusedAsInvalid(string link)
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        BadLinkException refused = await Assert.ThrowsAsync<BadLinkException>(() =>
            this._service.SetLinks(ada.Token, [link], this._cancellation)
        );

        // Assert
        Assert.Equal(
            (GameRequestFailure.Invalid, true),
            (refused.Failure, refused.Message.StartsWith($"'{link}' is not a web address", StringComparison.Ordinal))
        );
    }

    [Fact]
    public async Task SetLinks_WhenOneIsTooLong_IsRefused()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string link = "https://a.example/" + new string('a', PlayerService.MaximumLinkLength);

        // Act
        Task<Player?> setting = this._service.SetLinks(ada.Token, [link], this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<BadLinkException>(() => setting);
    }

    [Fact]
    public async Task SetLinks_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange - nothing beyond the empty store

        // Act
        Player? shown = await this._service.SetLinks("not a token", ["https://a.example"], this._cancellation);

        // Assert
        Assert.Null(shown);
    }

    [Fact]
    public async Task Profile_WhenThePlayerIsUnknown_IsNoOne()
    {
        // Arrange - nothing beyond the empty store

        // Act
        Player? profile = await this._service.Profile(PlayerId.New(), this._cancellation);

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task AiToken_WhenTheTokenStandsForAPlayer_IsAnAisTokenStandingForThem()
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);

        // Act
        string? issued = await this._service.AiToken(ada.Token, this._cancellation);

        // Assert
        Assert.Equal(
            (true, ada.Player.Id),
            (PlayerService.IsAiToken(issued), (await this._service.Find($"{issued}", this._cancellation))?.Id)
        );
    }

    [Fact]
    public async Task AiToken_WhenTheTokenStandsForNoOne_IsNone()
    {
        // Arrange - nothing beyond the empty store

        // Act
        string? issued = await this._service.AiToken("not a token", this._cancellation);

        // Assert
        Assert.Null(issued);
    }

    [Theory]
    [InlineData("ai token")]
    [InlineData("link code")]
    [InlineData("links")]
    [InlineData("unlink")]
    [InlineData("rename")]
    [InlineData("delete")]
    public async Task Changing_WhenTheTokenIsAnAis_IsForbidden(string change)
    {
        // Arrange
        SignedIn ada = await this._service.SignIn(Login, "Ada", this._cancellation);
        string ai = await this._service.AiToken(ada.Token, this._cancellation) ?? throw new InvalidOperationException();

        // Act
        AiTokenRefusedException refused = await Assert.ThrowsAsync<AiTokenRefusedException>(() =>
            change switch
            {
                "ai token" => this._service.AiToken(ai, this._cancellation),
                "link code" => this._service.LinkCode(ai, this._cancellation),
                "links" => this._service.SetLinks(ai, [], this._cancellation),
                "unlink" => this._service.Unlink(ai, Login.Provider, this._cancellation),
                "rename" => this._service.Rename(ai, "Bot", this._cancellation),
                _ => this._service.Delete(ai, this._cancellation),
            }
        );

        // Assert
        Assert.Equal(GameRequestFailure.Forbidden, refused.Failure);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("seat-token", false)]
    [InlineData("ai-anything", true)]
    public void IsAiToken_WhenGivenAToken_SaysWhetherItBeginsAsAnAisDoes(string? token, bool expected)
    {
        // Arrange - nothing

        // Act
        bool isAi = PlayerService.IsAiToken(token);

        // Assert
        Assert.Equal(expected, isAi);
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

    private async Task<string> CodeFor(SignedIn signedIn) =>
        await this._service.LinkCode(signedIn.Token, this._cancellation) ?? throw new InvalidOperationException();
}
