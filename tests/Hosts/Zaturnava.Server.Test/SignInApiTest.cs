using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>Signing in with a service, in a window of its own, and asking who a player's token stands for.</summary>
public sealed partial class SignInApiTest(SigningInServer server) : IClassFixture<SigningInServer>
{
    private readonly HttpClient _http =
        server.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Providers_WhenOneIsSetUp_AreItsName()
    {
        // Arrange - nothing beyond the server

        // Act
        ImmutableList<string>? providers =
            await this._http.GetFromJsonAsync<ImmutableList<string>>(GameRoutes.SignInProviders, this._cancellation);

        // Assert
        Assert.Equal(["GitHub"], providers);
    }

    [Fact]
    public async Task SignIn_WhenTheServiceIsSetUpAndThePageServed_SendsThePersonToTheService()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("GitHub", SigningInServer.Page),
            this._cancellation
        );

        // Assert
        string location = response.Headers.Location?.AbsoluteUri ?? string.Empty;
        Assert.Equal(
            (HttpStatusCode.Redirect, true, true),
            (
                response.StatusCode,
                location.StartsWith("https://github.com/login/oauth/authorize?", StringComparison.Ordinal),
                location.Contains("signin-github", StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("Elsewhere", SigningInServer.Page, HttpStatusCode.NotFound)]
    [InlineData("GitHub", "https://elsewhere.example", HttpStatusCode.BadRequest)]
    public async Task SignIn_WhenTheServiceIsNotSetUpOrThePageNotServed_Refuses(
        string provider,
        string origin,
        HttpStatusCode refusal
    )
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this._http.GetAsync(GameRoutes.SignIn(provider, origin), this._cancellation);

        // Assert
        Assert.Equal(refusal, response.StatusCode);
    }

    [Theory]
    [InlineData("GitHub|1|Ada One", "Ada One")]
    [InlineData("GitHub|2", "Player")]
    public async Task Done_WhenTheServiceVouchedForSomeone_PostsTheirTokenToThePage(string vouched, string name)
    {
        // Arrange - nothing beyond the person vouched for

        // Act
        string page = await this.Done(vouched, SigningInServer.Page);

        // Assert
        PlayerDto? player = await this.Me(TokenOf(page));
        Assert.Equal(
            (name, true),
            (player?.Name, page.Contains("""}, "http://localhost:5165");""", StringComparison.Ordinal))
        );
    }

    [Fact]
    public async Task Done_WhenTheSameLoginComesBack_SignsInTheSamePlayer()
    {
        // Arrange
        PlayerDto? first = await this.Me(TokenOf(await this.Done("GitHub|3|Ada", SigningInServer.Page)));

        // Act
        PlayerDto? again = await this.Me(TokenOf(await this.Done("GitHub|3|Ada", SigningInServer.Page)));

        // Assert
        Assert.Equal(
            (first?.Id, first?.Name, "GitHub"),
            (again?.Id, again?.Name, string.Join(',', again?.Services ?? []))
        );
    }

    [Theory]
    [InlineData(null, SigningInServer.Page, HttpStatusCode.Unauthorized)]
    [InlineData("GitHub", SigningInServer.Page, HttpStatusCode.Unauthorized)]
    [InlineData("GitHub|4|Ada", "https://elsewhere.example", HttpStatusCode.BadRequest)]
    public async Task Done_WhenNoOneWasVouchedForOrThePageIsNotServed_Refuses(
        string? vouched,
        string origin,
        HttpStatusCode refusal
    )
    {
        // Arrange
        using HttpRequestMessage request = DoneRequest(vouched, origin);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(refusal, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-token")]
    public async Task Me_WhenTheTokenStandsForNoOne_IsUnauthorized(string? token)
    {
        // Arrange
        using HttpRequestMessage request = new(HttpMethod.Get, GameRoutes.Me);
        request.Headers.Authorization =
            token is null ? null : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);

        // Act
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignOut_WhenTheTokenStandsForAPlayer_LetsItGoForGood()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|5|Ada", SigningInServer.Page));

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Delete, GameRoutes.Session, token);

        // Assert
        using HttpResponseMessage me = await this.Send(HttpMethod.Get, GameRoutes.Me, token);
        Assert.Equal((HttpStatusCode.NoContent, HttpStatusCode.Unauthorized), (response.StatusCode, me.StatusCode));
    }

    [Fact]
    public async Task Delete_WhenTheTokenStandsForAPlayer_ForgetsThemSoTheirLoginMakesANewOne()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|6|Ada", SigningInServer.Page));
        PlayerDto? before = await this.Me(token);

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Delete, GameRoutes.Me, token);

        // Assert
        PlayerDto? after = await this.Me(TokenOf(await this.Done("GitHub|6|Ada", SigningInServer.Page)));
        Assert.Equal((HttpStatusCode.NoContent, false), (response.StatusCode, before?.Id == after?.Id));
    }

    [Fact]
    public async Task Claim_WhenAPlayerSignedInTakesASeat_ShowsTheirNameThereUntilTheyAreDeleted()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|7|Ada Seven", SigningInServer.Page));
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        _ = await client.Claim(created.GameId, 0, token, new ClaimSeatRequest("Impostor"), this._cancellation);

        // Assert
        string? named = (await client.Get(created.GameId, this._cancellation)).Seats[0].Player;
        using HttpResponseMessage deleted = await this.Send(HttpMethod.Delete, GameRoutes.Me, token);
        GamePageDto page = await client.List(null, this._cancellation);
        Assert.Equal(
            ("Ada Seven", PlayerService.Deleted),
            (named, page.Games.Single(game => game.Id == created.GameId).Players[0])
        );
    }

    [Fact]
    public async Task Prefer_WhenAPlayerKeepsASetting_ShowsItWithThemFromThenOn()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|prefers|Ada", SigningInServer.Page));
        HttpGameClient client = server.GameClient();

        // Act
        PlayerDto? preferred = await client.Prefer(token, new PreferRequest("theme", "light"), this._cancellation);

        // Assert
        Assert.Equal(
            ("light", "light"),
            (
                preferred?.Preferences.GetValueOrDefault("theme"),
                (await client.Me(token, this._cancellation))?.Preferences.GetValueOrDefault("theme")
            )
        );
    }

    [Fact]
    public async Task Prefer_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange
        HttpGameClient client = server.GameClient();

        // Act
        PlayerDto? preferred = await client.Prefer("no one", new PreferRequest("theme", "light"), this._cancellation);

        // Assert
        Assert.Null(preferred);
    }

    [Fact]
    public async Task Claim_WhenTheGameIsForPlayersOnly_RefusesAGuestButSeatsAPlayerSignedIn()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|for-players|Ada Players", SigningInServer.Page));
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat) { IsForPlayers = true },
            this._cancellation
        );
        bool listed = (await client.List(new GameListQuery(PageSize: 100), this._cancellation))
            .Games.Single(game => game.Id == created.GameId).IsForPlayers;

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Claim(created.GameId, 0, null, new ClaimSeatRequest("Guest"), this._cancellation)
        );
        _ = await client.Claim(created.GameId, 0, token, null, this._cancellation);

        // Assert
        Assert.Equal(
            (true, Refusal.Forbidden, "Ada Players"),
            (listed, refused.Refusal, (await client.Get(created.GameId, this._cancellation)).Seats[0].Player)
        );
    }

    [Fact]
    public async Task Claim_WhenThePlayerTokenStandsForNoOne_IsRefusedRatherThanSeatingAGuest()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Claim(created.GameId, 0, "signed-out", null, this._cancellation)
        );

        // Assert
        Assert.Equal(
            (Refusal.Forbidden, 1),
            (refused.Refusal, (await client.Get(created.GameId, this._cancellation)).Seats.Count(IsOpen))
        );

        static bool IsOpen(SeatDto seat) => seat.Controller == "open";
    }

    [Fact]
    public async Task Identify_WhenAGuestSignsInAfterTakingASeat_ShowsTheirNameThere()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        SeatClaimedDto seat = await client.Claim(created.GameId, 0, null, null, this._cancellation);
        string token = TokenOf(await this.Done("GitHub|8|Ada Eight", SigningInServer.Page));

        // Act
        await client.Identify(created.GameId, 0, token, new IdentifySeatRequest(seat.Token), this._cancellation);

        // Assert
        SeatDto shown = (await client.Get(created.GameId, this._cancellation)).Seats[0];
        Assert.Equal(("Ada Eight", false), (shown.Player, shown.IsGuest));
    }

    [Fact]
    public async Task Identify_WhenAGuestChoosesANicknameAfterTakingASeat_ShowsItThere()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        SeatClaimedDto seat = await client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act
        await client.Identify(
            created.GameId,
            0,
            null,
            new IdentifySeatRequest(seat.Token, "Hal"),
            this._cancellation
        );

        // Assert
        Assert.Equal("Hal (guest)", (await client.Get(created.GameId, this._cancellation)).Seats[0].Player);
    }

    [Fact]
    public async Task TakeBack_WhenThePlayerClaimedTheSeatOnAnotherDevice_HandsThemATokenAndTheOldOneNoMore()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        string player = TokenOf(await this.Done("GitHub|taking-back|Ada", SigningInServer.Page));
        SeatClaimedDto old = await client.Claim(created.GameId, 0, player, null, this._cancellation);

        // Act
        SeatClaimedDto taken = await client.TakeBack(created.GameId, 0, player, this._cancellation);

        // Assert
        await client.Identify(created.GameId, 0, player, new IdentifySeatRequest(taken.Token), this._cancellation);
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Identify(created.GameId, 0, player, new IdentifySeatRequest(old.Token), this._cancellation)
        );
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task List_WhenAPlayerSignedInHoldsASeat_SaysWhoUnlessTheirAiPlaysIt(bool isAi, bool isShown)
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        string name = isAi ? "Imogen" : "Ignatius";
        string player = TokenOf(await this.Done($"GitHub|{name}|{name}", SigningInServer.Page));
        _ = await client.Claim(created.GameId, 0, player, new ClaimSeatRequest(null, isAi), this._cancellation);
        string? id = (await this.Me(player))?.Id;

        // Act
        GamePageDto page = await client.List(new GameListQuery(Players: name), this._cancellation);

        // Assert
        Assert.Equal(
            [isShown ? id : null, null, null, null],
            page.Games.Single(game => game.Id == created.GameId).PlayerIds
        );
    }

    [Fact]
    public async Task List_WhenAskedByAPlayersId_ListsOnlyTheGamesTheyClaimedASeatAtSignedIn()
    {
        // Arrange - a game of theirs, a guest's and another player's
        HttpGameClient client = server.GameClient();
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        string theirs = (await client.Create(request, this._cancellation)).GameId;
        string guests = (await client.Create(request, this._cancellation)).GameId;
        string others = (await client.Create(request, this._cancellation)).GameId;
        string player = TokenOf(await this.Done("GitHub|mine|Ada", SigningInServer.Page));
        string other = TokenOf(await this.Done("GitHub|not-mine|Cy", SigningInServer.Page));
        _ = await client.Claim(theirs, 0, player, null, this._cancellation);
        _ = await client.Claim(guests, 0, null, new ClaimSeatRequest("Una"), this._cancellation);
        _ = await client.Claim(others, 0, other, null, this._cancellation);
        string? id = (await this.Me(player))?.Id;

        // Act
        GamePageDto page = await client.List(new GameListQuery(Player: id), this._cancellation);

        // Assert
        Assert.Equal([theirs], page.Games.Select(game => game.Id));
    }

    [Fact]
    public async Task Awaiting_WhenAGameWaitsForThePlayersMove_ListsItWithTheirSeat()
    {
        // Arrange - the last seat taken, its player has the opening die to play
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        string player = TokenOf(await this.Done("GitHub|awaited|Ada Awaited", SigningInServer.Page));
        _ = await client.Claim(created.GameId, 0, player, null, this._cancellation);

        // Act
        ImmutableList<AwaitedGameDto>? awaited = await client.Awaiting(player, this._cancellation);

        // Assert
        Assert.Equal(
            [(created.GameId, 0, "Ada Awaited")],
            awaited?.Select(game => (game.Game.Id, game.Seat, game.Game.Players[0])) ?? []
        );
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("someone else")]
    public async Task TakeBack_WhenTheTokenIsNotTheClaimants_IsRefused(string who)
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        string ada = TokenOf(await this.Done("GitHub|claimant|Ada", SigningInServer.Page));
        _ = await client.Claim(created.GameId, 0, ada, null, this._cancellation);
        string token = who == "someone else"
            ? TokenOf(await this.Done("GitHub|not-the-claimant|Bob", SigningInServer.Page))
            : who;

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.TakeBack(created.GameId, 0, token, this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    [Fact]
    public async Task TakeOver_WhenAPlayerTakesABotsSeat_HoldsItByTheTokenGivenAndIsNamedThere()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        _ = await client.Claim(created.GameId, 0, null, null, this._cancellation);
        string player = TokenOf(await this.Done("GitHub|taking-over|Tamsin", SigningInServer.Page));

        // Act
        SeatClaimedDto taken = await client.TakeOver(created.GameId, 1, player, null, this._cancellation);

        // Assert
        await client.Identify(created.GameId, 1, player, new IdentifySeatRequest(taken.Token), this._cancellation);
        GameViewDto view = await client.Get(created.GameId, this._cancellation);
        Assert.Equal<(string?, string?)>((null, "Tamsin"), (view.Seats[1].BotId, view.Seats[1].Player));
    }

    [Theory]
    [InlineData(false, "Gus (guest)")]
    [InlineData(true, null)]
    public async Task TakeOver_WhenAGuestTakesABotsSeat_SeatsThemUnlessTheGameIsForPlayersOnly(
        bool isForPlayers,
        string? expected
    )
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat)
            {
                IsForPlayers = isForPlayers,
            },
            this._cancellation
        );

        // Act
        Refusal? refusal = null;
        try
        {
            _ = await client.TakeOver(created.GameId, 1, null, new ClaimSeatRequest("Gus"), this._cancellation);
        }
        catch (GameRequestRefusedException refused)
        {
            refusal = refused.Refusal;
        }

        // Assert
        GameViewDto view = await client.Get(created.GameId, this._cancellation);
        Assert.Equal<(string?, Refusal?)>(
            (expected, isForPlayers ? Refusal.Forbidden : null),
            (view.Seats[1].Player, refusal)
        );
    }

    [Fact]
    public async Task TakeBack_WhenNoPlayerTokenIsSent_IsRefused()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this.Send(HttpMethod.Post, "api/games/g/seats/0/take-back", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Identify_WhenTheTokenDoesNotHoldTheSeat_IsRefused()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        _ = await client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Identify(created.GameId, 0, null, new IdentifySeatRequest("not-its-token"), this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    [Theory]
    [InlineData("DELETE", "api/players/me", null)]
    [InlineData("DELETE", "api/players/me", "not-a-token")]
    [InlineData("DELETE", "api/players/me/session", null)]
    [InlineData("DELETE", "api/players/me/logins/GitHub", null)]
    [InlineData("DELETE", "api/players/me/logins/GitHub", "not-a-token")]
    [InlineData("POST", "api/players/me/link-code", null)]
    [InlineData("POST", "api/players/me/link-code", "not-a-token")]
    [InlineData("PUT", "api/players/me", null)]
    [InlineData("PUT", "api/players/me", "not-a-token")]
    [InlineData("POST", "api/players/me/ai-token", null)]
    [InlineData("POST", "api/players/me/ai-token", "not-a-token")]
    [InlineData("GET", "api/players/me/awaiting", null)]
    [InlineData("GET", "api/players/me/awaiting", "not-a-token")]
    public async Task Request_WhenTheTokenIsMissingOrStandsForNoOne_IsUnauthorized(
        string method,
        string route,
        string? token
    )
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response =
            await this.Send(new HttpMethod(method), route, token, new RenameRequest("Ada"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rename_WhenTheTokenStandsForAPlayer_NamesThemAnew()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|9|Ada", SigningInServer.Page));

        // Act
        PlayerDto? renamed = await server.GameClient().Rename(token, "Countess", this._cancellation);

        // Assert
        Assert.Equal(("Countess", "Countess"), (renamed?.Name, (await this.Me(token))?.Name));
    }

    [Fact]
    public async Task TakeAlias_WhenAPlayerGoesByTheName_HoldsTheNextFreeForTheGuest()
    {
        // Arrange
        _ = await this.Done("GitHub|31|Zelda", SigningInServer.Page);

        // Act
        GuestAliasDto taken = await server.GameClient().TakeAlias("zelda", null, this._cancellation);

        // Assert
        GuestAliasDto? held = await server.GameClient().MyAlias(taken.Token, this._cancellation);
        Assert.Equal(("zelda 2", "zelda 2"), (taken.Alias, held?.Alias));
    }

    [Fact]
    public async Task TakeAlias_WhenTheGuestHoldsOneAlready_LetsItGoForTheNewOneByTheSameToken()
    {
        // Arrange
        GuestAliasDto first = await server.GameClient().TakeAlias("Yorick", null, this._cancellation);

        // Act
        GuestAliasDto second = await server.GameClient().TakeAlias("Xena", first.Token, this._cancellation);

        // Assert
        GuestAliasDto other = await server.GameClient().TakeAlias("Yorick", null, this._cancellation);
        Assert.Equal(("Xena", first.Token, "Yorick"), (second.Alias, second.Token, other.Alias));
    }

    [Fact]
    public async Task MyAlias_WhenTheTokenHoldsNone_IsNone()
    {
        // Arrange - nothing beyond the server

        // Act
        GuestAliasDto? held = await server.GameClient().MyAlias("held by no one", this._cancellation);

        // Assert
        Assert.Null(held);
    }

    [Fact]
    public async Task Claim_WhenAGuestGivesAPlayersName_IsRefusedAsTaken()
    {
        // Arrange
        _ = await this.Done("GitHub|ulysses|Ulysses", SigningInServer.Page);
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Claim(created.GameId, 0, null, new ClaimSeatRequest("ulysses"), this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Conflict, refused.Refusal);
    }

    [Fact]
    public async Task Claim_WhenTheGuestSendsTheTokenTheyHoldTheirAliasBy_ShowsThemByIt()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GuestAliasDto held = await client.TakeAlias("Theodora", null, this._cancellation);
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Claim(created.GameId, 0, null, new ClaimSeatRequest("Theodora"), this._cancellation)
        );

        // Act
        _ = await client.Claim(
            created.GameId,
            0,
            null,
            new ClaimSeatRequest("Theodora", AliasToken: held.Token),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (Refusal.Conflict, Nicknames.Shown("Theodora")),
            (refused.Refusal, (await client.Get(created.GameId, this._cancellation)).Seats[0].Player)
        );
    }

    [Fact]
    public async Task MyAlias_WhenNoTokenIsSent_IsUnauthorized()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this.Send(HttpMethod.Get, GameRoutes.MyAlias, null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rename_WhenAGuestHoldsTheNameAsTheirAlias_IsRefused()
    {
        // Arrange
        string token = TokenOf(await this.Done("GitHub|32|Wanda", SigningInServer.Page));
        _ = await server.GameClient().TakeAlias("Valerie", null, this._cancellation);

        // Act
        Task renaming = server.GameClient().Rename(token, "valerie", this._cancellation);

        // Assert
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() => renaming);
        Assert.Equal(Refusal.Conflict, refused.Refusal);
    }

    [Fact]
    public async Task SignIn_WhenGivenACodeToLinkWith_SendsThePersonToTheServiceStill()
    {
        // Arrange - nothing beyond the server

        // Act
        using HttpResponseMessage response = await this._http.GetAsync(
            GameRoutes.SignIn("GitHub", SigningInServer.Page, "c0de"),
            this._cancellation
        );

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Done_WhenGivenACodeToLinkWith_LinksTheLoginToThePlayerWhoAskedForIt()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|10|Ada", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);

        // Act
        string page = await this.Done("Google|10|Ada G", SigningInServer.Page, code);

        // Assert
        PlayerDto? linked = await this.Me(TokenOf(page));
        PlayerDto? again = await this.Me(TokenOf(await this.Done("Google|10|Someone", SigningInServer.Page)));
        Assert.Equal(
            ("GitHub,Google", (await this.Me(token))?.Id, linked?.Id),
            (string.Join(',', linked?.Services ?? []), linked?.Id, again?.Id)
        );
    }

    [Fact]
    public async Task Done_WhenTheLoginToLinkIsAnotherPlayers_MergesThatPlayerIntoThePlayerWhoAskedForIt()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string bea = TokenOf(await this.Done("Google|11|Bea", SigningInServer.Page));
        string token = TokenOf(await this.Done("GitHub|11|Ada Eleven", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);

        // Act
        string page = await this.Done("Google|11|Bea", SigningInServer.Page, code);

        // Assert
        PlayerDto? linked = await this.Me(TokenOf(page));
        Assert.Equal(
            ("GitHub,Google", "Ada Eleven", (await this.Me(token))?.Id, linked?.Id),
            (string.Join(',', linked?.Services ?? []), linked?.Name, linked?.Id, (await this.Me(bea))?.Id)
        );
    }

    [Fact]
    public async Task Done_WhenTheCodeToLinkWithIsNoGood_SaysSoAndSignsNoOneIn()
    {
        // Arrange - no such code was ever issued

        // Act
        string page = await this.Done("Google|13|Bea", SigningInServer.Page, "no-such-code");

        // Assert
        Assert.Equal(
            (true, string.Empty),
            (page.Contains("That link has run out, or was used already", StringComparison.Ordinal), TokenOf(page))
        );
    }

    [Fact]
    public async Task Unlink_WhenThePlayerHasAnotherService_LetsItGoButNotTheLast()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|12|Ada", SigningInServer.Page));
        string? code = await client.LinkCode(token, this._cancellation);
        _ = await this.Done("Google|12|Ada", SigningInServer.Page, code);

        // Act
        PlayerDto? unlinked = await client.Unlink(token, "Google", this._cancellation);

        // Assert
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Unlink(token, "GitHub", this._cancellation)
        );
        Assert.Equal(("GitHub", Refusal.Conflict), (string.Join(',', unlinked?.Services ?? []), refused.Refusal));
    }

    [Fact]
    public async Task Profile_WhenThePlayerIsKnown_ShowsTheirNameToAnyone()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|13|Ada Thirteen", SigningInServer.Page));
        PlayerDto? ada = await client.Me(token, this._cancellation);

        // Act
        ProfileDto? profile = await client.Profile($"{ada?.Id}", this._cancellation);

        // Assert
        Assert.Equal(("Ada Thirteen", ada?.Id), (profile?.Name, profile?.Id));
    }

    [Fact]
    public async Task Describe_WhenAPlayerDescribesThemselves_ShowsItAndTheServicesChosenToAnyone()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|describes|Ada Describes", SigningInServer.Page));

        // Act
        PlayerDto? described =
            await client.Describe(token, new DescribeRequest("Counts.", ["GitHub", "Google"]), this._cancellation);

        // Assert
        ProfileDto? profile = await client.Profile($"{described?.Id}", this._cancellation);
        Assert.Equal(
            ("Counts.", "GitHub", "Counts.", "GitHub"),
            (
                described?.Description,
                string.Join(",", described?.Shown ?? []),
                profile?.Description,
                string.Join(",", profile?.Services ?? [])
            )
        );
    }

    [Fact]
    public async Task Describe_WhenTheTokenStandsForNoOne_IsNoOne()
    {
        // Arrange
        HttpGameClient client = server.GameClient();

        // Act
        PlayerDto? described = await client.Describe("no one", new DescribeRequest("Hi.", null), this._cancellation);

        // Assert
        Assert.Null(described);
    }

    [Fact]
    public async Task Profile_WhenNoSuchPlayerIsKnown_IsNone()
    {
        // Arrange - nothing beyond the server

        // Act
        ProfileDto? profile = await server.GameClient().Profile("nobody", this._cancellation);

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task AiToken_WhenItClaimsASeat_TheSeatIsTheirsShownAsPlayedByAnAi()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|15|Ada", SigningInServer.Page));
        string ai = await client.AiToken(token, this._cancellation) ?? throw new InvalidOperationException();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        _ = await client.Claim(created.GameId, 0, ai, null, this._cancellation);

        // Assert
        SeatDto seat = (await client.Get(created.GameId, this._cancellation)).Seats[0];
        Assert.Equal(
            (true, (await this.Me(token))?.Id),
            (seat.IsAi, seat.PlayerId)
        );
    }

    [Fact]
    public async Task Claim_WhenTheClaimSaysAnAiPlaysWithoutSigningIn_ShowsTheSeatAsAnAiGuests()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        GameCreatedDto created = await client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        _ = await client.Claim(created.GameId, 0, null, new ClaimSeatRequest("Hal", IsAi: true), this._cancellation);

        // Assert
        SeatDto seat = (await client.Get(created.GameId, this._cancellation)).Seats[0];
        Assert.Equal((true, null), (seat.IsAi, seat.PlayerId));
    }

    [Fact]
    public async Task AiToken_WhenAskedToChangeTheAccount_IsForbidden()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        string token = TokenOf(await this.Done("GitHub|16|Ada", SigningInServer.Page));
        string ai = await client.AiToken(token, this._cancellation) ?? throw new InvalidOperationException();

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.Rename(ai, "Bot", this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    private async Task<HttpResponseMessage> Send(
        HttpMethod method,
        string route,
        string? token,
        object? body = null
    )
    {
        using HttpRequestMessage request = new(method, route);
        request.Content = body is null ? null : JsonContent.Create(body);
        request.Headers.Authorization =
            token is null ? null : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        return await this._http.SendAsync(request, this._cancellation);
    }

    private static HttpRequestMessage DoneRequest(string? vouched, string origin, string? link = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "signin/done?origin=" + Uri.EscapeDataString(origin));
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.Header, vouched);
        _ = request.Headers.TryAddWithoutValidation(VouchedHandler.LinkHeader, link);
        return request;
    }

    private static string TokenOf(string page) => PostedToken().Match(page).Groups[1].Value;

    [GeneratedRegex("\"token\":\"([^\"]+)\"")]
    private static partial Regex PostedToken();

    private async Task<string> Done(string vouched, string origin, string? link = null)
    {
        using HttpRequestMessage request = DoneRequest(vouched, origin, link);
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);
        return await response.Content.ReadAsStringAsync(this._cancellation);
    }

    private async Task<PlayerDto?> Me(string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, GameRoutes.Me);
        request.Headers.Authorization = new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        using HttpResponseMessage response = await this._http.SendAsync(request, this._cancellation);
        return await response.Content.ReadFromJsonAsync<PlayerDto>(this._cancellation);
    }
}
