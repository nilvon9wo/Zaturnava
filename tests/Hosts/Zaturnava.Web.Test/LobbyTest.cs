using System.Collections.Immutable;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LobbyTest : IDisposable
{
    private static readonly ImmutableList<RulesetDto> GrandSizes =
    [
        Sized("grand-legions", "Grand Legions", "small"),
        Sized("grand-legions", "Grand Legions", "large"),
    ];

    private static readonly ImmutableList<RulesetDto> PreviewSizes =
        [.. GrandSizes.Select(ruleset => ruleset with { IsPreview = true })];

    private readonly PageContext _page = new();
    private Seating? _seated;

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Custom_WhenChosen_OffersANewGameOfOneSizeAndTheGamesToJoin()
    {
        // Arrange - nothing beyond the server's catalog and games
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", 0, 3, "Join as Yellow", FakeGameServer.GameId),
            (
                lobby.Find("p.rules").TextContent,
                lobby.FindAll("select.size, button.seat-by-seat").Count,
                lobby.FindAll("select.strength").Count,
                lobby.Find("button.join").GetAttribute("aria-label"),
                lobby.Find(".games-table tbody tr").GetAttribute("data-game")
            )
        );
    }

    [Fact]
    public void Render_WhenShown_TellsThatCustomIsTheWayToPlayOtherPeople()
    {
        // Arrange - nothing beyond the server's catalog and games

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Click Custom to play other people.", lobby.Find("p.setup-hint").TextContent.Trim());
    }

    [Fact]
    public void Render_WhenAGameIsAPreview_NeitherOffersNorListsIt()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", false, "chaturaji-2027", false),
            (
                lobby.Find("p.rules").TextContent,
                lobby.Find("input.show-previews").HasAttribute("checked"),
                this._page.Server.Listed?.Game,
                this._page.Server.Listed?.Preview
            )
        );
    }

    [Fact]
    public void Render_WhenNoGameIsAPreview_OffersNoChoiceToShowThem()
    {
        // Arrange - nothing beyond the server's catalog

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("input.show-previews"));
    }

    [Fact]
    public void ShowPreviews_WhenTicked_OffersAndListsThemNamedSoAndRemembersIt()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.show-previews").Change(true);

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions (preview)", "chaturaji-2027", null, "shown"),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                lobby.Find("select.game").GetAttribute("value"),
                this._page.Server.Listed?.Preview,
                $"{this._page.JSInterop.Invocations["zaturnava.save"]
                    .Last(saved => $"{saved.Arguments[0]}" == "zaturnava.previews")
                    .Arguments[1]}"
            )
        );
    }

    [Fact]
    public void ShowPreviews_WhenAPreviewOfSeveralSizesIsChosen_KeepsTheChoiceWithTheGameForTheWholeLobby()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("input.show-previews").Change(true);

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            ("game,show-previews", "size", "grand-legions"),
            (
                ClassesOf(lobby, ".lobby-games select, .lobby-games input"),
                ClassesOf(lobby, ".choices select, .choices input"),
                this._page.Server.Listed?.Game
            )
        );
    }

    [Fact]
    public void ShowPreviews_WhenUntickedWithOneChosenAndListed_GoesBackToTheFirstGameShownAndAnyInTheList()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.previews").SetResult("shown");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.last-game").SetResult("grand-legions");
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.filter-size").Change("grand-legions-large");
        string chosen = $"{lobby.Find("select.game").GetAttribute("value")}";

        // Act
        lobby.Find("input.show-previews").Change(false);

        // Assert
        Assert.Equal(
            ("grand-legions", "Chaturaji 2027", Waiting()),
            (chosen, lobby.Find("p.rules").TextContent, this._page.Server.Listed)
        );
    }

    [Fact]
    public void ShowPreviews_WhenUntickedWithAnotherChosenAndListed_KeepsBoth()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.previews").SetResult("shown");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.show-previews").Change(false);

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", Waiting()),
            (lobby.Find("p.rules").TextContent, this._page.Server.Listed)
        );
    }

    [Fact]
    public void Render_WhenEveryGameIsAPreview_OffersThemAllNamedSo()
    {
        // Arrange
        this._page.Server.ArePreviewsOnly = true;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Chaturaji 2027 (preview)", lobby.Find("p.rules").TextContent);
    }

    [Fact]
    public void Game_WhenOnePlayedAtSeveralSizesIsChosen_OffersItsSizesApart()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions", "small|large", "grand-legions-small"),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                string.Join('|', lobby.FindAll("select.size option").Select(option => option.TextContent)),
                lobby.Find("select.size").GetAttribute("value")
            )
        );
    }

    [Fact]
    public void Game_WhenChosenBackAndForth_KeepsEachGamesSizeChosen()
    {
        // Arrange
        this._page.Server.MoreRulesets =
        [
            .. GrandSizes,
            Sized("other", "Other", "small"),
            Sized("other", "Other", "large"),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.size").Change("grand-legions-large");

        // Act
        lobby.Find("select.game").Change("other");
        string? other = lobby.Find("select.size").GetAttribute("value");
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(("other-small", "grand-legions-large"), (other, lobby.Find("select.size").GetAttribute("value")));
    }

    [Fact]
    public void Game_WhenPlayedAtThreeSizes_IsSetUpAtTheMiddleOne()
    {
        // Arrange
        this._page.Server.MoreRulesets =
        [
            Sized("grand-legions", "Grand Legions", "small"),
            Sized("grand-legions", "Grand Legions", "medium"),
            Sized("grand-legions", "Grand Legions", "large"),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal("grand-legions-medium", lobby.Find("select.size").GetAttribute("value"));
    }

    [Theory]
    [InlineData("grand-legions", "grand-legions")]
    [InlineData("gone", "chaturaji-2027")]
    [InlineData(null, "chaturaji-2027")]
    public void Render_WhenAGameWasStartedBefore_SetsThatGameUpIfStillOffered(string? last, string expected)
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.last-game").SetResult(last);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find("select.game").GetAttribute("value"));
    }

    [Fact]
    public void Render_WhenTheGameWasSetUpBefore_SetsItUpAsLastStarted()
    {
        // Arrange - Green played; Black medium (Midas), Yellow the player's own AI, Red hard, Oracle chosen
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;2;1,-2,-1,2;,,,oracle");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("2", "Your ally, Black|Enemy, Red|Enemy, Yellow", "1|2|-2", "oracle"),
            (
                lobby.Find("select.colour").GetAttribute("value"),
                string.Join('|', SeatNames(lobby).Split('|').Skip(1)),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value"))),
                lobby.Find("select.pick").GetAttribute("value")
            )
        );
    }

    [Theory]
    [InlineData("chaturaji-2027;4;1,0,-1,2;,,,")]
    [InlineData("chaturaji-2027;-1;1,0,-1,2;,,,")]
    [InlineData("chaturaji-2027;0;1,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,0,1,2;,,")]
    [InlineData("chaturaji-2027;0;-1,3,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,-3,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,1,1,2;,,,")]
    [InlineData("gone;0;-1,0,1,2;,,,")]
    [InlineData("chaturaji-2027;0")]
    public void Render_WhenTheSetupRememberedNoLongerFits_SetsTheGameUpAfresh(string remembered)
    {
        // Arrange - a colour, a strength or a bot count the game no longer has; medium (Midas alone) twice
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult(remembered);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("0", "1|0|2"),
            (
                lobby.Find("select.colour").GetAttribute("value"),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value")))
            )
        );
    }

    [Fact]
    public void Render_WhenTheBotRememberedIsNoLongerOffered_ChoosesAnyOfItsStrength()
    {
        // Arrange - Red hard, with a bot no longer offered
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,0,1,2;,,,gone");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(string.Empty, lobby.Find("select.pick").GetAttribute("value"));
    }

    [Theory]
    [InlineData("button.create")]
    [InlineData("button.quick.medium")]
    public void Start_WhenClicked_RemembersTheGameAndHowItWasSetUp(string start)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.colour").Change("2");
        lobby.Find(start == "button.create" ? "button.custom" : "button.quick-game").Click();

        // Act
        lobby.Find(start).Click();

        // Assert
        ImmutableDictionary<string, string> saved = this._page.JSInterop.Invocations["zaturnava.save"]
            .GroupBy(save => $"{save.Arguments[0]}", StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => $"{group.Last().Arguments[1]}", StringComparer.Ordinal);
        Assert.Equal(
            ("chaturaji-2027", true),
            (
                saved["zaturnava.last-game"],
                saved["zaturnava.setup-of.chaturaji-2027"].StartsWith("chaturaji-2027;2;", StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("player-token", "Bea", null, "player-token")]
    [InlineData(null, "Bea", "alias-1", null)]
    [InlineData(null, null, null, null)]
    public void Play_WhenSignedInOrNot_ClaimsTheSeatAsThePlayerIfAnyAndByTheNicknameAndAliasIfAny(
        string? player,
        string? nickname,
        string? alias,
        string? expected
    )
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult(player);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult(nickname);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.alias").SetResult(alias);
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (expected, nickname, alias),
            (this._page.Server.ClaimedAs, this._page.Server.ClaimedBy, this._page.Server.ClaimedWith)
        );
    }

    [Fact]
    public void Play_WhenAScenarioIsChosen_StartsItWithThePlayerToMoveAndBotsElsewhere()
    {
        // Arrange
        this._page.Server.Scenarios =
        [
            new ScenarioDto("back-to-back", "chaturaji-2027", "Back to back", "Two Pawns face each other.")
            {
                Turn = "green",
            },
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.scenarios").Click();
        lobby.Find("select.scenario").Change("back-to-back");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest? created = this._page.Server.Created;
        Assert.Equal(
            ("back-to-back", "chaturaji-2027", "bot|bot|person|bot", "Two Pawns face each other."),
            (
                created?.Scenario,
                created?.RulesetId,
                string.Join('|', created?.Bots.Select(bot => bot is null ? "person" : "bot") ?? []),
                lobby.Find("p.scenario-description").TextContent
            )
        );
    }

    [Fact]
    public void Play_WhenAScenarioIsStarted_RemembersIt()
    {
        // Arrange
        this._page.Server.Scenarios =
        [
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
            new ScenarioDto("back-to-back", "chaturaji-2027", "Back to back", "Two Pawns face each other."),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.scenarios").Click();
        lobby.Find("select.scenario").Change("back-to-back");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Contains(
            this._page.JSInterop.Invocations["zaturnava.save"],
            saved => $"{saved.Arguments[0]}" == "zaturnava.scenario" && $"{saved.Arguments[1]}" == "back-to-back"
        );
    }

    [Theory]
    [InlineData("back-to-back", "Two Pawns face each other.")]
    [InlineData("gone", "Doubles.")]
    [InlineData("", "Doubles.")]
    public void Render_WhenAScenarioWasStartedBefore_ChoosesItAgainIfStillOffered(string last, string described)
    {
        // Arrange (Stated 2026-11-19: "the scenario field should remember the last scenario I played")
        this._page.Server.Scenarios =
        [
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
            new ScenarioDto("back-to-back", "chaturaji-2027", "Back to back", "Two Pawns face each other."),
        ];
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("scenarios");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.scenario").SetResult(last);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(described, lobby.Find("p.scenario-description").TextContent);
    }

    [Fact]
    public void Play_WhenTheScenarioIsOfAnotherSize_StartsItAtThatSize()
    {
        // Arrange - a game of two sizes, the scenario at the one not chosen
        this._page.Server.MoreRulesets =
        [
            new RulesetDto("chaturaji-big", "Chaturaji 2027 (big)", 4)
            {
                Game = "chaturaji-2027",
                Size = "big",
                SeatNames = ["Black", "Yellow", "Green", "Red"],
            },
        ];
        this._page.Server.Scenarios = [new ScenarioDto("wide", "chaturaji-big", "Wide", "Room.")];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.scenarios").Click();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ("Wide (big)", "chaturaji-big", null),
            (
                lobby.Find("select.scenario option").TextContent,
                this._page.Server.Created?.RulesetId,
                this._page.Server.Created?.Bots[0]
            )
        );
    }

    [Fact]
    public void Play_WhenTheScenarioCannotBeRead_AsksForItAtTheGameChosen()
    {
        // Arrange
        this._page.Server.Scenarios = [new ScenarioDto("broken", string.Empty, "broken", "broken.json is broken.")];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.scenarios").Click();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ("broken", "chaturaji-2027"),
            (this._page.Server.Created?.Scenario, this._page.Server.Created?.RulesetId)
        );
    }

    [Fact]
    public void Lobby_WhenScenariosAreOfSeveralGames_OffersThoseOfTheChosenGameAndThoseUnreadable()
    {
        // Arrange
        this._page.Server.Scenarios =
        [
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
            new ScenarioDto("chain", "grand-legions-medium", "Chain", "Jumps."),
            new ScenarioDto("broken", string.Empty, "broken", "broken.json cannot be read."),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.scenarios").Click();

        // Assert
        Assert.Equal(
            ("escape,broken", 0, 0),
            (
                string.Join(
                    ",",
                    lobby.FindAll("select.scenario option").Select(option => option.GetAttribute("value"))
                ),
                lobby.FindAll("select.colour").Count,
                lobby.FindAll("button.quick").Count
            )
        );
    }

    [Fact]
    public void Lobby_WhenScenariosWereLastChosenButTheGameHasNone_SetsUpQuickGames()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("scenarios");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal((0, 3), (lobby.FindAll("button.scenarios").Count, lobby.FindAll("button.quick").Count));
    }

    [Theory]
    [InlineData(false, false, 0, false)]
    [InlineData(true, false, 1, false)]
    [InlineData(true, true, 1, true)]
    public void Play_WhenTheGameOffersPromotionAtTheSide_AsksForItIfChecked(
        bool isOffered,
        bool isChecked,
        int shown,
        bool expected
    )
    {
        // Arrange
        this._page.Server.MayPromoteAtSide = isOffered;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (isChecked)
        {
            lobby.Find("input.promotes-at-side").Change(true);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (shown, expected),
            (lobby.FindAll("input.promotes-at-side").Count, this._page.Server.Created?.PromotesAtSide)
        );
    }

    [Theory]
    [InlineData(false, false, 0, false)]
    [InlineData(true, false, 1, false)]
    [InlineData(true, true, 1, true)]
    public void Play_WhenTheGameRollsDice_AsksForItWithoutThemIfChecked(
        bool hasDice,
        bool isChecked,
        int shown,
        bool expected
    )
    {
        // Arrange
        this._page.Server.HasDice = hasDice;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (isChecked)
        {
            lobby.Find("input.no-dice").Change(true);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (shown, expected),
            (lobby.FindAll("input.no-dice").Count, this._page.Server.Created?.AreDiceOff)
        );
    }

    [Theory]
    [InlineData(false, "", 0, null)]
    [InlineData(true, "", 1, null)]
    [InlineData(true, "2", 1, 2)]
    public void Play_WhenWithoutDice_AsksForTheOpenerChosenOrNoneForOneAtRandom(
        bool isOff,
        string chosen,
        int shown,
        int? expected
    )
    {
        // Arrange - the user's request of 2026-11-19
        this._page.Server.HasDice = true;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("input.no-dice").Change(isOff);
        if (isOff)
        {
            lobby.Find("select.opener").Change(chosen);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (shown, expected),
            (lobby.FindAll("select.opener").Count, this._page.Server.Created?.Opener)
        );
    }

    [Theory]
    [InlineData(null, "", 0, null)]
    [InlineData(3, "", 1, null)]
    [InlineData(3, "5", 1, 5)]
    [InlineData(3, "0", 1, null)]
    public void Play_WhenTheGameLetsItsCreatorSetIdleRounds_AsksForThoseTyped(
        int? offered,
        string typed,
        int shown,
        int? expected
    )
    {
        // Arrange
        this._page.Server.IdleRounds = offered;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (typed.Length > 0)
        {
            lobby.Find("input.idle-rounds").Change(typed);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (shown, expected),
            (lobby.FindAll("input.idle-rounds").Count, this._page.Server.Created?.IdleRounds)
        );
    }

    [Theory]
    [InlineData(3, 4)]
    [InlineData(null, null)]
    public void Render_WhenIdleRoundsWereSetBefore_SetsThemAgainIfTheGameStillLetsThem(int? offered, int? expected)
    {
        // Arrange
        this._page.Server.IdleRounds = offered;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,1,1,1;,,,;idle=4");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(expected, this._page.Server.Created?.IdleRounds);
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("9", null)]
    public void Render_WhenAnOpenerWasChosenBefore_ChoosesItAgainIfTheGameHasTheSeat(string seat, int? expected)
    {
        // Arrange
        this._page.Server.HasDice = true;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult($"chaturaji-2027;0;-1,1,1,1;,,,;dice-off=1,opener={seat}");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(expected, this._page.Server.Created?.Opener);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Render_WhenDiceWereTurnedOffBefore_TurnsThemOffAgainIfTheGameStillRollsThem(
        bool hasDice,
        bool expected
    )
    {
        // Arrange
        this._page.Server.HasDice = hasDice;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,1,1,1;,,,;dice-off=1");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(expected, this._page.Server.Created?.AreDiceOff);
    }

    /// <summary>Rivers parting the board one way or both (bits 1, 2), through the kingdoms or around (bit 4).</summary>
    private static readonly ImmutableList<TerrainAxisDto> Rivers =
    [
        new("Rivers", 0b011, [new("None", 0), new("↕ Bisect", 0b001), new("✚ Quarter", 0b011)]),
        new("Rivers flow", 0b100, [new("Through kingdoms", 0), new("Around kingdoms", 0b100)]),
    ];

    [Theory]
    [InlineData("", "", 0, 0)]
    [InlineData("3", "", 0b011, 0b011)]
    [InlineData("3", "4", 0b111, 0b111)]
    [InlineData("", "0", 0b100, 0)]
    public void Play_WhenTheCreatorChoosesTheRiversOrLeavesThemToChance_AsksForThemSo(
        string rivers,
        string course,
        int mask,
        int value
    )
    {
        // Arrange - asked 2026-11-19
        this._page.Server.TerrainAxes = Rivers;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        IReadOnlyList<AngleSharp.Dom.IElement> axes = lobby.FindAll("select.terrain-axis");
        axes[0].Change(rivers);
        lobby.FindAll("select.terrain-axis")[1].Change(course);

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (2, mask, value),
            (axes.Count, this._page.Server.Created?.TerrainMask, this._page.Server.Created?.TerrainValue)
        );
    }

    [Fact]
    public void Render_WhenTheRiversWereChosenBefore_ChoosesThemAgain()
    {
        // Arrange - quartered, around the kingdoms, then the game set up afresh
        this._page.Server.IsDealt = true;
        this._page.Server.TerrainAxes = Rivers;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,1,1,1;,,,;terrain=7:7");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ("3", "4", 0b111, 0b111),
            (
                lobby.FindAll("select.terrain-axis")[0].GetAttribute("value"),
                lobby.FindAll("select.terrain-axis")[1].GetAttribute("value"),
                this._page.Server.Created?.TerrainMask,
                this._page.Server.Created?.TerrainValue
            )
        );
    }

    [Fact]
    public void Render_WhenTheGamesOtherChoicesWereMadeBefore_MakesThemAgain()
    {
        // Arrange - drafted in secret, from allies too, for 150; at the side; on a 12 by 10 board
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;
        this._page.Server.MayPromoteAtSide = true;
        this._page.Server.SizableBoard = new BoardSizeDto(8, 8, 4, 2);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult(
                "chaturaji-2027;0;-1,1,1,1;,,,;armies=draft-own,budget=150,secret=1,hidden=1,side=1,board=12x10x4x3"
            );

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest? created = this._page.Server.Created;
        Assert.Equal(
            (true, 150, true, true, true, new BoardSizeDto(12, 10, 4, 3)),
            (
                created?.IsDrafted,
                created?.DraftBudget,
                created?.IsDraftSecret,
                created?.IsDraftHiddenFromAllies,
                created?.PromotesAtSide,
                created?.Board
            )
        );
    }

    [Theory]
    [InlineData("armies=draft-one,budget=9999,side=1", "random-each", 0)]
    [InlineData("armies=random-one,board=12x10x4x3", "random-one", 0)]
    [InlineData("armies=nonsense,players=1", "random-each", 0)]
    public void Render_WhenAChoiceRememberedIsNoLongerOffered_LeavesItAsTheGameHasIt(
        string choices,
        string armies,
        int sides
    )
    {
        // Arrange - dealt, but not drafted, nor promoting at the side; a guest, so not for players only
        this._page.Server.IsDealt = true;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,1,1,1;,,,;" + choices);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (armies, sides, false, false),
            (
                lobby.Find("select.armies").GetAttribute("value"),
                lobby.FindAll("input.promotes-at-side").Count,
                this._page.Server.Created?.PromotesAtSide,
                this._page.Server.Created?.IsForPlayers
            )
        );
    }

    [Fact]
    public void Play_WhenNoScenarioIsChosen_StartsFromTheBeginning()
    {
        // Arrange
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal((null, 0), (this._page.Server.Created?.Scenario, lobby.FindAll("p.scenario-description").Count));
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    public void Play_WhenAPlayerIsSignedIn_OffersToBarGuests(bool isSignedIn, bool barsGuests, int offered)
    {
        // Arrange
        if (isSignedIn)
        {
            this.SignInAsAdaAtYellow();
        }

        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (barsGuests)
        {
            lobby.Find("input.players-only").Change(true);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (offered, barsGuests),
            (lobby.FindAll("input.players-only").Count, this._page.Server.Created!.IsForPlayers)
        );
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void Render_WhenAGameIsPlayedWithoutDice_SaysSoBesideIt(bool areDiceOff, int shown)
    {
        // Arrange
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { OpenSeats = [1], AreDiceOff = areDiceOff, PlayerIds = [] },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(shown, lobby.FindAll(".games-table tbody tr span.no-dice").Count);
    }

    [Theory]
    [InlineData(false, true, "Join")]
    [InlineData(true, false, "Join")]
    [InlineData(true, true, "Sign in to join")]
    public void Render_WhenAGameIsForPlayersOnly_AsksAGuestToSignInToJoin(bool isForPlayers, bool isGuest, string shown)
    {
        // Arrange
        if (!isGuest)
        {
            this.SignInAsAdaAtYellow();
        }

        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { OpenSeats = [1], IsForPlayers = isForPlayers, PlayerIds = [] },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            shown,
            lobby.Find(".games-table tbody tr :is(button.join, span.for-players)").TextContent.Trim()
        );
    }

    [Theory]
    [InlineData(false, false, null, null, "0 0 0 False False  ")]
    [InlineData(true, false, null, null, "1 2 0 False False  ")]
    [InlineData(true, false, "random-one", null, "1 2 0 True False  ")]
    [InlineData(true, true, null, null, "1 4 1 False True 75 ")]
    [InlineData(true, true, "draft-own", "30", "1 4 1 False True 30 ")]
    [InlineData(true, true, "draft-one", "30", "1 4 1 False True 30 0")]
    [InlineData(true, true, "draft-own", "", "1 4 1 False True  ")]
    [InlineData(true, true, "random-each", null, "1 4 1 False False 75 ")]
    [InlineData(true, true, "random-each", "40", "1 4 1 False False 40 ")]
    [InlineData(true, true, "random-one", "40", "1 4 1 True False 40 ")]
    public void Play_WhenTheArmiesAreDealt_AsksHowTheyComeToBe(
        bool isDealt,
        bool isDraftable,
        string? armies,
        string? budget,
        string expected
    )
    {
        // Arrange - expected: selects, options, budget boxes, alike, drafted, budget, drafter
        this._page.Server.IsDealt = isDealt;
        this._page.Server.IsDraftable = isDraftable;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (armies is not null)
        {
            lobby.Find("select.armies").Change(armies);
        }

        if (budget is not null)
        {
            lobby.Find("input.draft-budget").Change(budget);
        }

        int boxes = lobby.FindAll("input.draft-budget").Count;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal(
            (expected, (string?)null),
            (
                $"{lobby.FindAll("select.armies").Count} {lobby.FindAll("select.armies option").Count} {boxes} "
                    + $"{created.AreArmiesAlike} {created.IsDrafted} {created.DraftBudget} {created.Drafter}",
                created.ArmiesOf
            )
        );
    }

    [Theory]
    [InlineData("draft-own", true, "1 True")]
    [InlineData("draft-own", false, "1 False")]
    [InlineData("draft-one", null, "0 False")]
    public void Play_WhenEachDraftsTheirOwnArmy_AsksWhetherInSecret(string armies, bool? isSecret, string expected)
    {
        // Arrange - expected: secret boxes, drafted in secret
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.armies").Change(armies);
        if (isSecret is { } secret)
        {
            lobby.Find("input.draft-secret").Change(secret);
        }

        int boxes = lobby.FindAll("input.draft-secret").Count;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(expected, $"{boxes} {this._page.Server.Created!.IsDraftSecret}");
    }

    [Theory]
    [InlineData(true, true, "1 True True")]
    [InlineData(true, false, "1 True False")]
    [InlineData(false, null, "0 False False")]
    public void Play_WhenDraftingInSecret_AsksWhetherFromAlliesToo(bool isSecret, bool? fromAllies, string expected)
    {
        // Arrange - expected: allies boxes, drafted in secret, from allies too
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.armies").Change("draft-own");
        lobby.Find("input.draft-secret").Change(isSecret);
        if (fromAllies is { } hidden)
        {
            lobby.Find("input.draft-hidden-from-allies").Change(hidden);
        }

        int boxes = lobby.FindAll("input.draft-hidden-from-allies").Count;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal(expected, $"{boxes} {created.IsDraftSecret} {created.IsDraftHiddenFromAllies}");
    }

    [Fact]
    public void Play_WhenTheBoardAndKingdomsAreSet_AsksForThemAndGrowsTheBudgetWithTheKingdoms()
    {
        // Arrange - 75 points for kingdoms of 4 x 3, at most 600, so 112 for 6 x 3, at most 900
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;
        this._page.Server.SizableBoard = new BoardSizeDto(10, 10, 4, 3);
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        string before = lobby.Find("input.board-width").GetAttribute("value") ?? string.Empty;
        lobby.Find("input.board-width").Change("12");
        lobby.Find("input.board-height").Change("14");
        lobby.Find("input.kingdom-width").Change("6");
        lobby.Find("input.kingdom-depth").Change("3");
        string budget = lobby.Find("input.draft-budget").GetAttribute("value") ?? string.Empty;
        string most = lobby.Find("input.draft-budget").GetAttribute("max") ?? string.Empty;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal(
            ("10", "112", "900", new BoardSizeDto(12, 14, 6, 3), (int?)112),
            (before, budget, most, created.Board, created.DraftBudget)
        );
    }

    [Theory]
    [InlineData("8", true)]
    [InlineData("7", false)]
    public void Play_WhenTheKingdomsWouldOverlapOrNot_SaysSoAndAsksForNoGameOrPlays(string width, bool isWrong)
    {
        // Arrange - a 10 x 10 board, kingdoms 3 deep (playtest 2026-11-15, H3)
        this._page.Server.IsDealt = true;
        this._page.Server.SizableBoard = new BoardSizeDto(10, 10, 4, 3);
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("input.kingdom-width").Change(width);

        // Assert
        Assert.Equal(
            (isWrong, isWrong, isWrong),
            (
                lobby.FindAll(".board-problem").Count == 1,
                lobby.Find("button.create").HasAttribute("disabled"),
                lobby.FindAll(".board-problem")
                    .Any(problem => problem.TextContent.Contains("(11)", StringComparison.Ordinal))
            )
        );
    }

    [Fact]
    public void Play_WhenTheKingdomsAreSetInAGameNotDrafted_AsksForNoBudget()
    {
        // Arrange - armies dealt at random, never drafted
        this._page.Server.IsDealt = true;
        this._page.Server.SizableBoard = new BoardSizeDto(10, 10, 4, 3);
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("input.kingdom-width").Change("5");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal((new BoardSizeDto(10, 10, 5, 3), (int?)null), (created.Board, created.DraftBudget));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Quick_WhenTheBoardMayBeSet_StartsOneOnTheBoardAsSet(bool isSizable)
    {
        // Arrange
        this._page.Server.SizableBoard = isSizable ? new BoardSizeDto(10, 10, 4, 3) : null;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.quick.medium").Click();

        // Assert
        Assert.Equal(
            (isSizable ? new BoardSizeDto(10, 10, 4, 3) : null, isSizable ? 1 : 0),
            (this._page.Server.Created!.Board, lobby.FindAll("table.board-size input.board-height").Count)
        );
    }

    [Fact]
    public void Render_WhenTheArmiesAreDrafted_ShowsTheGamesOwnBudgetToSpendAndTheMostAllowed()
    {
        // Arrange - the fake server's drafted game gives each seat 75 points, and at most 600
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        AngleSharp.Dom.IElement box = lobby.Find("input.draft-budget");
        Assert.Equal(("75", "600"), (box.GetAttribute("value"), box.GetAttribute("max")));
    }

    [Fact]
    public void Render_WhenTheServerOffersNoScenarios_OffersNoChoiceOfStart()
    {
        // Arrange - the fake server offers none

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Empty(lobby.FindAll("select.scenario"));
    }

    [Fact]
    public void Quick_WhenAnotherGameAndSizeAreChosen_StartsOneOfThatGameAtThatSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.size").Change("grand-legions-large");
        lobby.Find("button.quick.medium").Click();

        // Assert
        Assert.Equal("grand-legions-large", this._page.Server.Created?.RulesetId);
    }

    [Fact]
    public void Render_WhenNeverSetUpBefore_OffersAQuickGameAlone()
    {
        // Arrange
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert - no Play, no plans and no scenario: each difficulty starts a game at once
        Assert.Equal(
            ("true", "Easy|Medium|Hard", 0),
            (
                lobby.Find("button.quick-game").GetAttribute("aria-pressed"),
                string.Join('|', lobby.FindAll("button.quick").Select(quick => quick.TextContent)),
                lobby.FindAll("button.create, select.strength, select.plan, select.scenario").Count
            )
        );
    }

    [Fact]
    public void Render_WhenSetUpBefore_SetsUpAsLastChosen()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("true", true, 0),
            (
                lobby.Find("button.custom").GetAttribute("aria-pressed"),
                lobby.FindAll("select.strength").Count > 0,
                lobby.FindAll("button.quick").Count
            )
        );
    }

    [Fact]
    public void SetUp_WhenAModeIsChosen_RemembersIt()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();
        lobby.Find("button.quick-game").Click();

        // Assert
        Assert.Equal(
            "strength|quick",
            string.Join(
                '|',
                this._page.JSInterop.Invocations["zaturnava.save"]
                    .Where(save => save.Arguments[0] is "zaturnava.setup")
                    .Select(save => save.Arguments[1])
            )
        );
    }

    [Fact]
    public void Quick_WhenClicked_SitsAtSomeSeatWithEachBotOnceAmongTheOthers()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.quick.medium").Click();

        // Assert - never Claude, which is paid for
        ImmutableList<string?> bots = this._page.Server.Created!.Bots;
        Assert.Equal(
            (3, 3, true, false),
            (
                bots.OfType<string>().Count(),
                bots.OfType<string>().Distinct(StringComparer.Ordinal).Count(),
                bots[this._seated!.Claim!.Seat] is null,
                bots.Contains("claude")
            )
        );
    }

    [Theory]
    [InlineData("easy", "midas,random,solomon")]
    [InlineData("hard", "midas,oracle,solomon")]
    public void Quick_WhenEasyOrHard_SeatsTheEasiestOrHardestBots(string difficulty, string expected)
    {
        // Arrange - Random is rated easiest, then Midas, Solomon and Oracle; Claude, paid for, is never seated
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find($"button.quick.{difficulty}").Click();

        // Assert
        Assert.Equal(
            expected,
            string.Join(",", this._page.Server.Created!.Bots.OfType<string>().Order(StringComparer.Ordinal))
        );
    }

    [Fact]
    public void Render_WhenLoaded_OffersByStrengthTheColourThenTheAllyAndEnemiesNearestMediumWithABotLeft()
    {
        // Arrange - Easy is Random alone, Medium Midas alone, Hard Solomon and Oracle; the ally gets Medium, an
        // enemy the easier of the two strengths as near, the other the one left

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Equal(
            (
                "You play|Your ally, Green|Enemy, Yellow|Enemy, Red",
                "1|0|2",
                "Person|Your own AI|Easy bot (Elo 1000)|Medium bot (Elo 1100)|Hard bot (Elo 1250–1400)",
                0
            ),
            (
                SeatNames(lobby),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value"))),
                string.Join('|', lobby.Find("select.strength").Children.Select(option => option.TextContent.Trim())),
                lobby.FindAll("select.plan").Count
            )
        );
    }

    [Fact]
    public void ByStrength_WhenAStrengthsBotsAllHaveSeats_OffersItToNoOtherSeat()
    {
        // Arrange - Green is Medium, Midas alone; Yellow Easy, Random alone; Red Hard, of two
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.FindAll("select.strength")[1].Change("2");

        // Assert - Hard's two bots seat Yellow and Red, Medium's one Green; Easy's is free again
        Assert.Equal(
            ("hard", "medium", "medium"),
            (
                DisabledLevels(lobby, 0),
                DisabledLevels(lobby, 1),
                DisabledLevels(lobby, 2)
            )
        );
    }

    [Fact]
    public void ByStrength_WhenTheColourLeftHasNoBotLeftAtItsStrength_PlaysItAtTheNearestWithOne()
    {
        // Arrange - Black, the player's, would be Hard; Yellow and Red take both Hard bots
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("2");

        // Act - Green, Medium, becomes the player's, and Black is left
        lobby.Find("select.colour").Change("2");

        // Assert - seats in turn after Green: Black, its ally, then Yellow and Red
        Assert.Equal(
            "1|2|2",
            string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value")))
        );
    }

    [Fact]
    public void ByStrength_WhenTheColourLeftWasForAPerson_KeepsItForAPerson()
    {
        // Arrange - Yellow is left to a person, then played, then left again
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("-1");
        lobby.Find("select.colour").Change("1");

        // Act
        lobby.Find("select.colour").Change("0");

        // Assert
        Assert.Equal("-1", lobby.FindAll("select.strength")[1].GetAttribute("value"));
    }

    [Fact]
    public void Play_WhenASeatIsForTheirOwnAi_LeavesItOpenHavingSaidHowToConnectIt()
    {
        // Arrange - Yellow is left for the player's own AI
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("-2");
        string hint = lobby.Find("p.own-ai a").GetAttribute("href")!;

        // Act
        lobby.Find("button.create").Click();

        // Assert - the player's seat and Yellow open; bots in the other two
        Assert.Equal(
            (DocsLinks.Bots, (string?)null, 2),
            (hint, this._page.Server.Created!.Bots[1], this._page.Server.Created.Bots.OfType<string>().Count())
        );
    }

    [Fact]
    public void ByStrength_WhenNoSeatIsForTheirOwnAi_SaysNothingOfConnectingOne()
    {
        // Arrange - nothing beyond the seats set up by strength

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Empty(lobby.FindAll("p.own-ai"));
    }

    [Fact]
    public void Play_WhenABotWasChosenForTheColourTakenSinceAndForAnotherSeat_SeatsItOnce()
    {
        // Arrange - Oracle chosen for Red; Red taken, so Black, left, may choose it; then Green taken instead
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.pick")[0].Change("oracle");
        lobby.Find("select.colour").Change("3");
        lobby.FindAll("select.pick")[0].Change("oracle");
        lobby.Find("select.colour").Change("2");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        ImmutableList<string> bots = [.. this._page.Server.Created!.Bots.OfType<string>()];
        Assert.Equal(bots.Count, bots.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ByStrength_WhenAnotherColourIsChosen_PutsItsAllyFirstThenItsEnemiesInTurn()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("select.colour").Change("1");

        // Assert
        Assert.Equal("You play|Your ally, Red|Enemy, Green|Enemy, Black", SeatNames(lobby));
    }

    [Fact]
    public void Play_WhenSetUpByStrength_SitsAtTheColourWithBotsAsStrongAsChosen()
    {
        // Arrange - Green's ally is Black, a person; Yellow is easy (Random), Red hard (Solomon or Oracle)
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.colour").Change("2");
        lobby.FindAll("select.strength")[0].Change("-1");
        lobby.FindAll("select.strength")[1].Change("2");
        lobby.FindAll("select.strength")[2].Change("0");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        ImmutableList<string?> bots = this._page.Server.Created!.Bots;
        Assert.Equal(
            ("random", true, 2),
            (bots[1], (bots[0], bots[2], bots[3]) is (null, null, "solomon" or "oracle"), this._seated!.Claim!.Seat)
        );
    }

    [Fact]
    public void ByStrength_WhenAStrengthHasSeveralBots_OffersAChoiceOfThemOrAny()
    {
        // Arrange - Black plays; the seats go Green, Yellow, Red; medium is Midas alone, hard Solomon and Oracle
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.FindAll("select.strength")[2].Change("2");

        // Assert
        Assert.Equal(
            (1, "Any of them|Solomon (Elo 1250)|Oracle (Elo 1400)"),
            (
                lobby.FindAll("select.pick").Count,
                string.Join('|', lobby.Find("select.pick").Children.Select(option => option.TextContent.Trim()))
            )
        );
    }

    [Fact]
    public void Play_WhenABotOfTheStrengthIsChosen_SeatsThatOne()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[2].Change("2");
        lobby.Find("select.pick").Change("oracle");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal("oracle", this._page.Server.Created!.Bots[3]);
    }

    [Fact]
    public void ByStrength_WhenABotIsChosenForOneSeat_CannotBeChosenForAnother()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("2");
        lobby.FindAll("select.strength")[2].Change("2");

        // Act
        lobby.FindAll("select.pick")[1].Change("oracle");

        // Assert
        Assert.Equal(
            (true, false),
            (
                lobby.FindAll("select.pick")[0].QuerySelector("option[value=oracle]")!.HasAttribute("disabled"),
                lobby.FindAll("select.pick")[1].QuerySelector("option[value=oracle]")!.HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void ByStrength_WhenTheStrengthChanges_ForgetsTheBotChosen()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[2].Change("2");
        lobby.Find("select.pick").Change("oracle");

        // Act
        lobby.FindAll("select.strength")[2].Change("-1");
        lobby.FindAll("select.strength")[2].Change("2");

        // Assert
        Assert.Equal(string.Empty, lobby.Find("select.pick").GetAttribute("value"));
    }

    [Fact]
    public void Custom_WhenChosenAfterQuick_OffersEachSeatsStrengthInsteadOfQuickGames()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();

        // Assert
        Assert.Equal((0, 3), (lobby.FindAll("button.quick").Count, lobby.FindAll("select.strength").Count));
    }

    [Fact]
    public void Join_WhenClicked_ClaimsThatSeat()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.join").Click();

        // Assert
        Assert.Equal(new Seating(FakeGameServer.GameId, new SeatClaim(1, "secret")), this._seated);
    }

    [Fact]
    public void Watch_WhenClicked_WatchesTheGame()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.watch").Click();

        // Assert
        Assert.Equal(Seating.Watching(FakeGameServer.GameId), this._seated);
    }

    [Fact]
    public void Render_WhenGamesAreListed_GivesEachSeatAColumnAndEachFactACell()
    {
        // Arrange - Ada away at Black, Yellow and Red open, a bot at Green (playtest 2026-11-15, H4)
        this._page.Server.Games = [this._page.Server.Games[0] with { Moves = 12, AwaySeats = [0] }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (
                "Black|Yellow|Green|Red|Next|Moves|Created|Last change|Actions",
                "Ada away|Join|random|Join|-|12|-|-"
            ),
            (
                string.Join('|', lobby.FindAll(".games-table th").Select(heading => heading.TextContent.Trim())),
                Cells(lobby, ".games-table tbody td:not(.game-actions)")
            )
        );
    }

    [Theory]
    [InlineData(false, 2, "Green")]
    [InlineData(true, 2, "-")]
    [InlineData(false, null, "-")]
    public void Render_WhenAGameIsUnderWay_SaysWhichSeatDecidesNext(bool isOver, int? deciding, string expected)
    {
        // Arrange - every seat taken, Green to decide, or the game over, or who decides not kept
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { OpenSeats = [], IsOver = isOver, DecidingSeat = deciding },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find(".games-table td.next").TextContent.Trim());
    }

    [Fact]
    public void Resume_WhenThisBrowserHeldASeat_GoesBackToIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal(new Seating(FakeGameServer.GameId, new SeatClaim(2, "kept")), this._seated);
    }

    [Fact]
    public void TakeBack_WhenThePlayerSignedInClaimedASeatThisBrowserDoesNotHold_TakesItBackForANewToken()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.take-back").Click();

        // Assert
        Assert.Equal(
            ("Back to Yellow", new Seating(FakeGameServer.GameId, new SeatClaim(1, FakeGameServer.Token))),
            (lobby.Find("button.take-back").TextContent.Trim(), this._seated)
        );
        Assert.Contains($"take back {FakeGameServer.GameId} 1 player", this._page.Server.Calls);
    }

    [Theory]
    [InlineData(null, "take back", FakeGameServer.Token)]
    [InlineData("1 kept", "resume", "kept")]
    [InlineData("2 kept", "take back", FakeGameServer.Token)]
    public void BackTo_WhenAGameWaitsForThePlayer_GoesBackToTheSeatHeldHereElseTakesItBack(
        string? held,
        string how,
        string token
    )
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        this._page.Server.Awaited = this._page.Server.Awaited.SetItem(
            "player",
            [new AwaitedGameDto(this._page.Server.Games[0], 1)]
        );
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult(held);
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.awaited-resume").Click();

        // Assert
        Assert.Equal(
            ("Back to Yellow", new Seating(FakeGameServer.GameId, new SeatClaim(1, token)), how == "take back"),
            (
                lobby.Find("button.awaited-resume").TextContent.Trim(),
                this._seated,
                this._page.Server.Calls.Contains($"take back {FakeGameServer.GameId} 1 player")
            )
        );
    }

    [Theory]
    [InlineData(false, 1, null, "")]
    [InlineData(true, 0, false, "hidden")]
    public void HideScenarios_WhenTickedOrNot_HidesGamesStartedFromScenariosFromBothListsAndRemembersIt(
        bool hidden,
        int awaited,
        bool? listed,
        string remembered
    )
    {
        // Arrange - a development server's, with a scenario game waiting for the player (the user's request of
        // 2026-11-19)
        this.SignInAsAdaAtYellow();
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];
        this._page.Server.Awaited = this._page.Server.Awaited.SetItem(
            "player",
            [new AwaitedGameDto(this._page.Server.Games[0] with { IsScenario = true }, 1)]
        );
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.hide-scenarios").Change(hidden);

        // Assert
        Assert.Equal(
            (awaited, listed, remembered),
            (
                lobby.FindAll(".awaited").Count,
                this._page.Server.Listed?.Scenario,
                $"{this._page.JSInterop.Invocations["zaturnava.save"]
                    .Last(saved => $"{saved.Arguments[0]}" == "zaturnava.games-list")
                    .Arguments[1]}".Contains("scenarios=hidden", StringComparison.Ordinal)
                    ? "hidden"
                    : string.Empty
            )
        );
    }

    [Fact]
    public void RemoveScenarioGames_WhenConfirmed_LetsGoOfThemAndListsTheGamesAgain()
    {
        // Arrange - a development server's, with a scenario game (the user's requests of 2026-11-19, and of
        // 2026-11-20: asked again before)
        this.SignInAsAdaAtYellow();
        this.WithAScenarioGame();
        IRenderedComponent<Lobby> lobby = this.Render();
        int listings = this._page.Server.Calls.Count(call => call == "list");
        lobby.Find("button.remove-scenario-games").Click();
        string asked = lobby.Find(".confirm-removal").TextContent;

        // Act
        lobby.Find("button.removal-confirmed").Click();

        // Assert
        Assert.Equal(
            (true, 1, listings + 1),
            (
                asked.Contains("Delete all 1 games started from scenarios?", StringComparison.Ordinal),
                this._page.Server.ScenarioGamesRemoved,
                this._page.Server.Calls.Count(call => call == "list")
            )
        );
    }

    [Fact]
    public void RemoveScenarioGames_WhenKept_DeletesNothingAndOffersItAgain()
    {
        // Arrange
        this.WithAScenarioGame();
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.remove-scenario-games").Click();

        // Act
        lobby.Find("button.removal-cancelled").Click();

        // Assert
        Assert.Equal(
            (0, 1, 0),
            (
                this._page.Server.ScenarioGamesRemoved,
                lobby.FindAll("button.remove-scenario-games").Count,
                lobby.FindAll(".confirm-removal").Count
            )
        );
    }

    [Fact]
    public void Render_WhenNoGameStartedFromAScenario_OffersNotToDeleteThem()
    {
        // Arrange - a development server's, its games none from scenarios
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert - hiding them stays offered
        Assert.Equal(
            (1, 0),
            (lobby.FindAll("input.hide-scenarios").Count, lobby.FindAll("button.remove-scenario-games").Count)
        );
    }

    [Fact]
    public void Render_WhenTheServerOffersNoScenarios_OffersNeitherToHideNorToDeleteScenarioGames()
    {
        // Arrange - a live server's

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("input.hide-scenarios, button.remove-scenario-games"));
    }

    /// <summary>A development server's: it offers a scenario, and one of its games started from it.</summary>
    private void WithAScenarioGame()
    {
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];
        this._page.Server.Games = [.. this._page.Server.Games.Select(game => game with { IsScenario = true })];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_WhenNoGameWaitsForThePlayer_ShowsNoneWaiting(bool isKnown)
    {
        // Arrange - the player's games told, none; or not told, the token standing for no one by then
        this.SignInAsAdaAtYellow();
        this._page.Server.Awaited = isKnown ? [] : this._page.Server.Awaited.SetItem("player", null);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll(".awaited"));
    }

    [Fact]
    public void Render_WhenThePlayerSignedInPlaysNoSeat_OffersNoneToTakeBack()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        this._page.Server.Players = this._page.Server.Players.SetItem("player", new PlayerDto("bob", "Bob"));

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("button.take-back"));
    }

    [Fact]
    public void TakeBack_WhenRefused_SaysWhyAndStaysInTheLobby()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Forbidden, "Not your seat.");

        // Act
        lobby.Find("button.take-back").Click();

        // Assert
        Assert.Equal(
            ((Seating?)null, "Not your seat."),
            (this._seated, lobby.Find("p.problem").TextContent)
        );
    }

    [Fact]
    public void Resume_WhenThePlayerSignedInOrChoseANicknameSince_SaysSoAtTheSeat()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult("player");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult("Ada");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal([$"{FakeGameServer.GameId} 2 kept player Ada"], this._page.Server.Identified);
    }

    [Fact]
    public void Resume_WhenTheSeatCannotBeSaidToBeHeldAnew_GoesBackToItAsItWas()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Forbidden, "Signed out.");

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal(
            (new Seating(FakeGameServer.GameId, new SeatClaim(2, "kept")), (string?)null),
            (this._seated, lobby.FindAll("p.problem").Count == 0 ? null : "a problem shown")
        );
    }

    [Fact]
    public void Render_WhenThisBrowserHeldASeat_NamesIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Back to Green", lobby.Find("button.resume").TextContent.Trim());
    }

    [Fact]
    public void Render_WhenAGameIsOver_OffersNothingToDoInIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        this._page.Server.Games = [this._page.Server.Games[0] with { IsOver = true }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Over", 0),
            (
                lobby.Find("span.over").TextContent,
                lobby.FindAll("button.watch, button.join, button.resume").Count
            )
        );
    }

    [Fact]
    public void Render_WhenTheRulesetNamesNoSeats_NumbersThem()
    {
        // Arrange
        this._page.Server.Games = [this._page.Server.Games[0] with { SeatNames = [] }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Join as seat 2", "seat 1"),
            (lobby.Find("button.join").GetAttribute("aria-label"), lobby.Find(".games-table th.seat").TextContent)
        );
    }

    [Fact]
    public void Render_WhenThisBrowserHeldNoSeat_OffersNoReturn()
    {
        // Arrange - nothing remembered

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("button.resume"));
    }

    [Fact]
    public void Render_WhenTheServerCannotBeReached_SaysWhy()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("The server is asleep.");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("The server is asleep.", 0),
            (lobby.Find("p.problem").TextContent, lobby.FindAll("button").Count)
        );
    }

    [Fact]
    public void Render_WhenLoaded_ListsTheFirstPageOfTheGamesWaitingForPlayers()
    {
        // Arrange - nothing beyond the server

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (Waiting(), "waiting", 0),
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"), Pages(lobby))
        );
    }

    [Theory]
    [InlineData("waiting", true)]
    [InlineData("in-progress", false)]
    [InlineData("not-over", null)]
    public void FilterState_WhenChosen_ListsTheGamesWaitingForPlayersOrUnderWayOrEither(string state, bool? open)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.filter-state").Change(state);

        // Assert
        Assert.Equal(
            (Waiting() with { Open = open }, state),
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"))
        );
    }

    [Fact]
    public void FilterState_WhenAPlayerSignedInChoosesMine_ListsTheirGamesUntilAnotherIsChosen()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();
        string offered =
            string.Join('|', lobby.FindAll("select.filter-state option").Select(option => option.TextContent));

        // Act
        lobby.Find("select.filter-state").Change("mine");
        (GameListQuery?, string?) mine =
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"));
        lobby.Find("select.filter-state").Change("waiting");

        // Assert
        Assert.Equal(
            (
                "Any not over|Waiting for players|In progress|Mine",
                (Waiting() with { Open = null, Player = "ada" }, "mine"),
                Waiting()
            ),
            (offered, mine, this._page.Server.Listed)
        );
    }

    [Fact]
    public void FilterState_WhenRendered_OffersNoGamesOver()
    {
        // Arrange - nothing beyond the server

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            "Any not over|Waiting for players|In progress",
            string.Join('|', lobby.FindAll("select.filter-state option").Select(option => option.TextContent))
        );
    }

    [Fact]
    public void Game_WhenRendered_OffersEachGameOnceWhateverItsSizesAndListsTheOneChosenWithoutAChoiceOfSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions", 0, 1, Waiting()),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                lobby.FindAll("select.filter-size").Count,
                lobby.FindAll("select.game").Count,
                this._page.Server.Listed
            )
        );
    }

    [Fact]
    public void Game_WhenOnePlayedAtSeveralSizesIsChosen_ListsItAtAnySizeAndShowsItsLeaderboard()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            (Waiting() with { Game = "grand-legions" }, "Any|small|large", "leaderboard grand-legions"),
            (
                this._page.Server.Listed,
                string.Join('|', lobby.FindAll("select.filter-size option").Select(option => option.TextContent)),
                this._page.Server.Calls.Last(call => call.StartsWith("leaderboard", StringComparison.Ordinal))
            )
        );
    }

    [Theory]
    [InlineData("grand-legions-large", "grand-legions-large")]
    [InlineData("", null)]
    public void FilterSize_WhenChosen_ListsTheGameAtThatSizeOrAny(string chosen, string? expected)
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");

        // Act
        lobby.Find("select.filter-size").Change(chosen);

        // Assert
        Assert.Equal(Waiting() with { Ruleset = expected, Game = "grand-legions" }, this._page.Server.Listed);
    }

    [Fact]
    public void Game_WhenOneOfOneSizeIsChosenAfterASize_ListsItAndOffersNoSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.filter-size").Change("grand-legions-large");

        // Act
        lobby.Find("select.game").Change("chaturaji-2027");

        // Assert
        Assert.Equal((Waiting(), 0), (this._page.Server.Listed, lobby.FindAll("select.filter-size").Count));
    }

    [Theory]
    [InlineData("chaturaji-2027", "")]
    [InlineData("retired", "Chaturaji 2027|retired")]
    [InlineData("chaturaji-2027-large", "Chaturaji 2027|large")]
    public void Render_WhenTheGamesListedDifferInSize_GivesEachItsSizeElseNone(string other, string expected)
    {
        // Arrange - a second game of the same ruleset, of one no longer offered, or of a larger size
        this._page.Server.MoreRulesets =
        [
            new RulesetDto("chaturaji-2027-large", "Chaturaji 2027 (large)", 4)
            {
                Game = "chaturaji-2027",
                GameName = "Chaturaji 2027",
                Size = "large",
            },
        ];
        GameSummaryDto game = this._page.Server.Games[0];
        this._page.Server.Games = [game, game with { Id = "other", RulesetId = other }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (expected, expected.Length > 0),
            (
                string.Join('|', lobby.FindAll(".games-table td.size").Select(cell => cell.TextContent)),
                lobby.FindAll(".games-table th.size").Count == 1
            )
        );
    }

    [Fact]
    public void Render_WhenTheGamesListedDifferInSize_TellsEachBoardKingdomsAndBudget()
    {
        // Arrange - a larger game on a 12 by 10 board, its kingdoms 4 by 3, each army spending 150; the other told
        // nothing of them (the user, 2026-11-20)
        this._page.Server.MoreRulesets =
        [
            new RulesetDto("chaturaji-2027-large", "Chaturaji 2027 (large)", 4)
            {
                Game = "chaturaji-2027",
                GameName = "Chaturaji 2027",
                Size = "large",
            },
        ];
        GameSummaryDto game = this._page.Server.Games[0];
        this._page.Server.Games =
        [
            game,
            game with
            {
                Id = "other",
                RulesetId = "chaturaji-2027-large",
                Board = new BoardSizeDto(12, 10, 4, 3),
                Budget = 150,
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("|12x10", "|4x3", "|150"),
            (
                string.Join('|', lobby.FindAll(".games-table td.board").Select(cell => cell.TextContent)),
                string.Join('|', lobby.FindAll(".games-table td.kingdom").Select(cell => cell.TextContent)),
                string.Join('|', lobby.FindAll(".games-table td.budget").Select(cell => cell.TextContent))
            )
        );
    }

    [Fact]
    public void Render_WhenASizeOfFixedBoardIsChosen_TellsItsBoardAndKingdoms()
    {
        // Arrange - the game at two sizes, the larger on a 12 by 12 board, its kingdoms 4 by 4 (the user, 2026-11-20)
        this._page.Server.MoreRulesets =
        [
            new RulesetDto("chaturaji-2027-large", "Chaturaji 2027 (large)", 4)
            {
                Game = "chaturaji-2027",
                GameName = "Chaturaji 2027",
                Size = "large",
                SeatNames = ["Black", "Yellow", "Green", "Red"],
                SeatTeams = [0, 1, 0, 1],
                Board = new BoardSizeDto(12, 12, 4, 4),
            },
        ];
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("select.size").Change("chaturaji-2027-large");

        // Assert
        Assert.Equal(
            "12 12 4 4",
            string.Join(' ', lobby.FindAll("table.board-size.fixed tbody td").Select(cell => cell.TextContent.Trim()))
        );
    }

    [Theory]
    [InlineData(false, "Ada away|Yellow away")]
    [InlineData(true, "Ada|Yellow")]
    public void Render_WhenSeatsAtAGameAreAway_MarksThemUnlessItIsOver(bool isOver, string expected)
    {
        // Arrange - Black and Yellow away, Yellow a person too
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = [],
                Players = ["Ada", "Yellow", "random", "Bo"],
                IsOver = isOver,
                AwaySeats = [0, 1],
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, Cells(lobby, ".games-table td.seat-0, .games-table td.seat-1"));
    }

    [Fact]
    public void Render_WhenAGamesTimesWereKept_SaysWhenItWasCreatedAndLastChangedEachInItsColumn()
    {
        // Arrange - well inside the hour and the minute they round down to, however slow the test
        DateTimeOffset now = DateTimeOffset.UtcNow;
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = [],
                Created = now.AddMinutes(-210),
                LastChanged = now.AddSeconds(-330),
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("3 h ago", "5 min ago"),
            (lobby.Find(".games-table td.created").TextContent, lobby.Find(".games-table td.changed").TextContent)
        );
    }

    [Fact]
    public void Render_WhenGamesAreListed_ShowsThePlayersOwnFirstThenTheLongestWaitingEachInARow()
    {
        // Arrange - Ada's game, signed in, changed last; one held in this browser, ten minutes ago; of the others, one
        // waited an hour, one never told (playtest 2026-11-15, H4)
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GameSummaryDto game = this._page.Server.Games[0];
        this.SignInAsAdaAtYellow();
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat.recent").SetResult("2 kept");
        this._page.Server.Games =
        [
            game with { Id = "recent", PlayerIds = [], LastChanged = now.AddMinutes(-10) },
            game with { Id = "untold", PlayerIds = [], LastChanged = null },
            game with { Id = "adas", PlayerIds = [null, "ada", null, null], LastChanged = now },
            game with { Id = "oldest", PlayerIds = [], LastChanged = now.AddHours(-1) },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("recent|adas|oldest|untold", "1 h ago"),
            (GameIds(lobby), lobby.FindAll(".games-table tbody td.changed")[2].TextContent.Trim())
        );
    }

    [Theory]
    [InlineData(new[] { "moves" }, "c|a|b", "ascending")]
    [InlineData(new[] { "moves", "moves" }, "b|a|c", "descending")]
    [InlineData(new[] { "next" }, "c|b|a", "ascending")]
    [InlineData(new[] { "created" }, "b|c|a", "ascending")]
    [InlineData(new[] { "next", "changed" }, "b|a|c", "ascending")]
    public void Sort_WhenAHeadingIsClicked_SortsByItAndAgainTheOtherWay(
        string[] clicked,
        string expected,
        string sort
    )
    {
        // Arrange - every seat taken at three games: b with 9 moves, Yellow next, created first; a with 5, Red next,
        // created last; c with 1, Black next; none told when it last changed, so kept in order on that tie
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GameSummaryDto game = this._page.Server.Games[0] with { OpenSeats = [], LastChanged = null };
        this._page.Server.Games =
        [
            game with { Id = "b", Moves = 9, DecidingSeat = 1, Created = now.AddHours(-3) },
            game with { Id = "a", Moves = 5, DecidingSeat = 3, Created = now },
            game with { Id = "c", Moves = 1, DecidingSeat = 0, Created = now.AddHours(-1) },
        ];
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        foreach (string column in clicked)
        {
            lobby.Find($"th button.sort-{column}").Click();
        }

        // Assert
        Assert.Equal(
            (expected, sort),
            (GameIds(lobby), lobby.Find($"th:has(button.sort-{clicked[^1]})").GetAttribute("aria-sort"))
        );
    }

    [Theory]
    [InlineData("seat-0", "c|d|a|b")]
    [InlineData("seat-1", "b|c|d|a")]
    [InlineData("seat-2", "a|b|c|d")]
    [InlineData("size", "a|d|c|b")]
    public void Sort_WhenASizeOrASeatHeadingIsClicked_SortsBySizeOrByWhoHoldsTheSeatOpenSeatsFirst(
        string column,
        string expected
    )
    {
        // Arrange - Black held by Bob at a, Zed at b, Ada at c, Al at d; Yellow open at b, and at d, over, so held by
        // no one, listed as Open; a and d of Chaturaji 2027, c small, b large; d of two seats, so last by the third
        this._page.Server.MoreRulesets = GrandSizes;
        GameSummaryDto game = this._page.Server.Games[0];
        this._page.Server.Games =
        [
            game with { Id = "a", Players = ["Bob", "Yan", "x", "y"], OpenSeats = [] },
            game with
            {
                Id = "b",
                RulesetId = "grand-legions-large",
                Players = ["Zed", GameSummaryDto.Open, "x", "y"],
                OpenSeats = [1],
            },
            game with
            {
                Id = "c",
                RulesetId = "grand-legions-small",
                Players = ["ada", "Kim", "x", "y"],
                OpenSeats = [],
            },
            game with { Id = "d", Players = ["Al", GameSummaryDto.Open], OpenSeats = [1], IsOver = true },
        ];
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find($"th button.sort-{column}").Click();

        // Assert
        Assert.Equal(expected, GameIds(lobby));
    }

    [Fact]
    public void FilterAndSort_WhenChanged_AreRememberedAsTheyAreLeft()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.filter-state").Change("in-progress");
        lobby.Find("input.filter-people").Change(true);
        lobby.Find("input.filter-players").Change("Bob, open");
        lobby.Find("th button.sort-moves").Click();
        lobby.Find("th button.sort-moves").Click();

        // Assert
        Assert.Equal(
            "desc=1&people=1&players=Bob%2C%20open&sort=moves&state=in-progress",
            $"{this._page.JSInterop.Invocations["zaturnava.save"]
                .Last(saved => $"{saved.Arguments[0]}" == "zaturnava.games-list")
                .Arguments[1]}"
        );
    }

    [Theory]
    [InlineData("state=in-progress&people=1&players=Bob&size=chaturaji-2027", null, false, true, "Bob", true)]
    [InlineData("state=mine&size=grand-legions-small", null, true, null, null, false)]
    [InlineData("state=mine", "player", null, null, null, false)]
    public void Render_WhenTheListWasLeftFilteredSo_ListsItSoAgain(
        string remembered,
        string? player,
        bool? open,
        bool? peopleOnly,
        string? players,
        bool isSized
    )
    {
        // Arrange - a size of the game chosen, or of another; Mine, signed in or not (then, waiting for players)
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.games-list").SetResult(remembered);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult(player);
        this._page.Server.Players = this._page.Server.Players.SetItem("player", new PlayerDto("ada", "Ada"));
        this._page.Server.MoreRulesets = GrandSizes;

        // Act
        _ = this.Render();

        // Assert
        Assert.Equal(
            (open, peopleOnly, players, isSized ? "chaturaji-2027" : null, player is null ? null : "ada"),
            (
                this._page.Server.Listed?.Open,
                this._page.Server.Listed?.PeopleOnly,
                this._page.Server.Listed?.Players,
                this._page.Server.Listed?.Ruleset,
                this._page.Server.Listed?.Player
            )
        );
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, null)]
    public void Render_WhenScenarioGamesWereLeftHidden_HidesThemAgainWhereScenariosAreOffered(
        bool offersScenarios,
        bool? expected
    )
    {
        // Arrange - a development server's, or one no longer offering scenarios
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.games-list").SetResult("scenarios=hidden");
        this._page.Server.Scenarios = offersScenarios
            ? [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")]
            : [];

        // Act
        _ = this.Render();

        // Assert
        Assert.Equal(expected, this._page.Server.Listed?.Scenario);
    }

    [Theory]
    [InlineData("sort=moves&desc=1", "b|a", "descending")]
    [InlineData("sort=seat-x", "a|b", "none")]
    public void Render_WhenTheListWasLeftSorted_SortsItSoAgain(string remembered, string expected, string sort)
    {
        // Arrange - a with 5 moves, b with 9; by moves, or by a column there is not
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.games-list").SetResult(remembered);
        GameSummaryDto game = this._page.Server.Games[0];
        this._page.Server.Games = [game with { Id = "a", Moves = 5 }, game with { Id = "b", Moves = 9 }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (expected, sort),
            (GameIds(lobby), lobby.Find("th:has(button.sort-moves)").GetAttribute("aria-sort"))
        );
    }

    [Fact]
    public void Render_WhenThePageIsInAFrame_HidesNewGameUntilAsked()
    {
        // Arrange - no game to join, which would show New game, but in itch.io's frame (reported 2026-11-19)
        this._page.Server.Games = [];
        _ = this._page.JSInterop.Setup<bool>("zaturnava.isFramed").SetResult(true);
        IRenderedComponent<Lobby> lobby = this.RenderAsIs();
        int before = lobby.FindAll(".panel.new-game").Count;

        // Act
        lobby.Find("button.new-game-toggle").Click();

        // Assert
        Assert.Equal((0, 1), (before, lobby.FindAll(".panel.new-game").Count));
    }

    [Fact]
    public void NewGame_WhenShown_FloatsAsADialogToDragThatItsCrossCloses()
    {
        // Arrange - no game to join, so New game is shown
        this._page.Server.Games = [];
        IRenderedComponent<Lobby> lobby = this.RenderAsIs();
        (string? Role, object? Key) shown = (
            lobby.Find(".panel.new-game").GetAttribute("role"),
            this._page.JSInterop.Invocations["zaturnava.draggable"].Single().Arguments[1]
        );

        // Act
        lobby.Find("button.close-new-game").Click();
        lobby.Find("button.new-game-toggle").Click();

        // Assert
        Assert.Equal(
            ("dialog", "zaturnava.new-game-place", 2),
            (shown.Role, shown.Key, this._page.JSInterop.Invocations["zaturnava.draggable"].Count)
        );
    }

    [Theory]
    [InlineData("open", false, false)]
    [InlineData("for players", false, true)]
    [InlineData("over", false, true)]
    [InlineData("full", false, true)]
    [InlineData("open", true, true)]
    public void Render_WhenThereAreGamesToJoinOrNot_HidesNewGameUntilAskedOrShowsIt(
        string listed,
        bool isToggled,
        bool isShown
    )
    {
        // Arrange - the game listed, with open seats, or for players only, over, or full (playtest 2026-11-15, H1)
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = listed == "full" ? [] : [1, 3],
                IsForPlayers = listed == "for players",
                IsOver = listed == "over",
            },
        ];
        IRenderedComponent<Lobby> lobby = this.RenderAsIs();

        // Act
        if (isToggled)
        {
            lobby.Find("button.new-game-toggle").Click();
        }

        // Assert
        Assert.Equal(
            (isShown, isShown ? "true" : "false"),
            (
                lobby.FindAll(".panel.new-game").Count == 1,
                lobby.Find("button.new-game-toggle").GetAttribute("aria-expanded")
            )
        );
    }

    [Fact]
    public void Render_WhenGamesWaitForThePlayer_ListsThemAboveTheGamesAsTheyAreListedAndTheLeaderboardRightmost()
    {
        // Arrange - Ada signed in, the game waiting on her at Yellow
        this.SignInAsAdaAtYellow();
        this._page.Server.Awaited = this._page.Server.Awaited.SetItem(
            "player",
            [new AwaitedGameDto(this._page.Server.Games[0], 1)]
        );

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("panel awaited|panel games-panel", "Back to Yellow", 1, true),
            (
                string.Join('|', lobby.FindAll(".games-column > .panel").Select(panel => panel.ClassName)),
                lobby.Find(".awaited .games-table button.awaited-resume").TextContent.Trim(),
                lobby.FindAll(".awaited .games-table th button.sort").Count == 0 ? 1 : 0,
                lobby.Find(".lobby").LastElementChild!.ClassList.Contains("leaderboard")
            )
        );
    }

    [Fact]
    public void Render_WhenAWaitingGameWasCreatedJustNow_SaysSoAndThatItWaitsOnNoOneToMove()
    {
        // Arrange
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { Created = DateTimeOffset.UtcNow, DecidingSeat = 0 },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("just now", "-"),
            (lobby.Find("td.created").TextContent, lobby.Find("td.next").TextContent.Trim())
        );
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, null)]
    public void FilterPeople_WhenTicked_ListsOnlyGamesWithNoBotsOrAny(bool ticked, bool? expected)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-people").Change(ticked);

        // Assert
        Assert.Equal(Waiting() with { PeopleOnly = expected }, this._page.Server.Listed);
    }

    [Theory]
    [InlineData("Bob, Jo*", "Bob, Jo*")]
    [InlineData("", null)]
    public void FilterPlayers_WhenSomeoneIsNamed_ListsOnlyGamesTheyAreAllAtOrAny(string named, string? expected)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-players").Change(named);

        // Assert
        Assert.Equal(Waiting() with { Players = expected }, this._page.Server.Listed);
    }

    [Fact]
    public void Render_WhileTheServerHasNotAnswered_SaysItIsReachingItAndMayTakeAWhile()
    {
        // Arrange
        this._page.Server.HeldCatalog = new TaskCompletionSource<CatalogDto>();

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (1, 1, 0),
            (
                lobby.FindAll(".reaching .spinner").Count,
                lobby.FindAll(".reaching[role='status']").Count,
                lobby.FindAll("p.problem").Count
            )
        );
    }

    [Fact]
    public void Render_WhenTheServerCannotBeReached_SaysSoAndNotThatItIsStillReachingIt()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("No server.");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal((1, 0), (lobby.FindAll("p.problem").Count, lobby.FindAll(".reaching").Count));
    }

    [Fact]
    public void FindByCode_WhenACodeIsEntered_ListsOnlyTheGameWhoseIdStartsSoWhateverTheFilters()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-code").Change("  3fa2c9d1 ");

        // Assert
        Assert.Equal(new GameListQuery(Code: "3fa2c9d1"), this._page.Server.Listed);
    }

    [Fact]
    public void Render_WhenThePageIsOpenedByAnInvitationsLink_ListsOnlyItsGameAndShowsItsCode()
    {
        // Arrange - the page served by the game server, opened at a game (Stated 2026-11-19, OQ9)
        this._page.Services.GetRequiredService<NavigationManager>().NavigateTo("play/?game=3fa2c9d1");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (new GameListQuery(Code: "3fa2c9d1"), "3fa2c9d1"),
            (this._page.Server.Listed, lobby.Find("input.filter-code").GetAttribute("value"))
        );
    }

    [Fact]
    public void FindByCode_WhenTheCodeIsCleared_ListsTheGamesTheFiltersAskForAgain()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("input.filter-code").Change("3fa2c9d1");

        // Act
        lobby.Find("input.filter-code").Change(" ");

        // Assert
        Assert.Equal(Waiting(), this._page.Server.Listed);
    }

    [Fact]
    public void Render_WhenNoGameMatches_SaysSo()
    {
        // Arrange
        this._page.Server.Games = [];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("No games like that.", lobby.Find("p.no-games").TextContent);
    }

    [Fact]
    public void Render_WhenMoreGamesMatchThanAPageHolds_OffersOnlyTheNextPage()
    {
        // Arrange
        this._page.Server.Total = 45;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Page 1 of 3 (45 games)", true, false),
            (
                lobby.Find("span.page-number").TextContent,
                lobby.Find("button.previous").HasAttribute("disabled"),
                lobby.Find("button.next").HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void Next_WhenClicked_ListsTheNextPage()
    {
        // Arrange
        this._page.Server.Total = 45;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.next").Click();

        // Assert
        Assert.Equal(
            (Waiting() with { Page = 2 }, "Page 2 of 3 (45 games)"),
            (this._page.Server.Listed, lobby.Find("span.page-number").TextContent)
        );
    }

    [Fact]
    public void Previous_WhenClickedOnTheLastPage_ListsThePageBeforeAndAFilterListsTheFirstAgain()
    {
        // Arrange
        this._page.Server.Total = 45;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.next").Click();
        lobby.Find("button.next").Click();
        bool isLast = lobby.Find("button.next").HasAttribute("disabled");

        // Act
        lobby.Find("button.previous").Click();
        GameListQuery? turned = this._page.Server.Listed;
        lobby.Find("select.filter-state").Change("in-progress");

        // Assert
        Assert.Equal(
            (true, 2, null),
            (isLast, turned?.Page, this._page.Server.Listed?.Page)
        );
    }

    [Fact]
    public void Filter_WhenTheServerCannotBeReached_SaysWhy()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new HttpRequestException("The server is asleep.");

        // Act
        lobby.Find("select.filter-state").Change("in-progress");

        // Assert
        Assert.Equal("The server is asleep.", lobby.Find("p.problem").TextContent);
    }

    private static int Pages(IRenderedComponent<Lobby> lobby) => lobby.FindAll("div.pages").Count;

    /// <summary>
    /// What the list asks for until the player filters it otherwise: the games waiting for players, of the game
    /// chosen, Chaturaji 2027 unless another.
    /// </summary>
    private static GameListQuery Waiting() => new(Open: true, Over: false, Game: "chaturaji-2027", Preview: false);

    /// <summary>The classes of the fields <paramref name="selector"/> finds, in order, by commas.</summary>
    private static string ClassesOf(IRenderedComponent<Lobby> lobby, string selector) =>
        string.Join(",", lobby.FindAll(selector).Select(field => field.ClassName));

    /// <summary>A ruleset that is one size of a game played at several: "Grand Legions (small)".</summary>
    private static RulesetDto Sized(string game, string gameName, string size) =>
        new($"{game}-{size}", $"{gameName} ({size})", 4) { Game = game, GameName = gameName, Size = size };

    private static string SeatNames(IRenderedComponent<Lobby> lobby) =>
        string.Join('|', lobby.FindAll("span.seat-name").Select(name => name.TextContent.Trim()));

    /// <summary>"easy,medium": the strengths the seat's choice offers but may not be chosen.</summary>
    private static string DisabledLevels(IRenderedComponent<Lobby> lobby, int seat) =>
        string.Join(
            ',',
            lobby.FindAll("select.strength")[seat].QuerySelectorAll("option[disabled]")
                .Select(option => option.TextContent.Trim().Split(' ')[0].ToLowerInvariant())
        );

    private IRenderedComponent<Lobby> ByStrength()
    {
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.custom").Click();
        return lobby;
    }

    /// <summary>The lobby, its New game panel shown, as a player opens it when there are games to join.</summary>
    private IRenderedComponent<Lobby> Render()
    {
        IRenderedComponent<Lobby> lobby = this.RenderAsIs();
        if (lobby.FindAll(".panel.new-game").Count == 0 && lobby.FindAll("button.new-game-toggle").Count == 1)
        {
            lobby.Find("button.new-game-toggle").Click();
        }

        return lobby;
    }

    /// <summary>The text of each cell <paramref name="selector"/> finds, single-spaced, joined by "|".</summary>
    private static string Cells(IRenderedComponent<Lobby> lobby, string selector) =>
        string.Join(
            '|',
            lobby.FindAll(selector)
                .Select(cell =>
                    string.Join(' ', cell.TextContent.Split(' ', '\n').Where(word => word.Length > 0))
                )
        );

    /// <summary>The games listed, by id, in the order shown.</summary>
    private static string GameIds(IRenderedComponent<Lobby> lobby) =>
        string.Join(
            '|',
            lobby.FindAll(".games-panel .games-table tbody tr").Select(row => row.GetAttribute("data-game"))
        );

    private IRenderedComponent<Lobby> RenderAsIs() =>
        this._page.Render<Lobby>(parameters => parameters.Add(x => x.OnSeated, seating => this._seated = seating));

    /// <summary>Ada signed in, by the token "player", playing Yellow themselves at the game listed.</summary>
    private void SignInAsAdaAtYellow()
    {
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult("player");
        this._page.Server.Players = this._page.Server.Players.SetItem("player", new PlayerDto("ada", "Ada"));
        this._page.Server.Games =
        [
            .. this._page.Server.Games.Select(game => game with { PlayerIds = [null, "ada", null, null] }),
        ];
    }
}
