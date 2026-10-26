using System.Collections.Immutable;
using System.Globalization;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class TableTest : IDisposable
{
    private static readonly Seating SeatZero = new(FakeGameServer.GameId, new SeatClaim(0, FakeGameServer.Token));
    private static readonly Seating SeatOne = new(FakeGameServer.GameId, new SeatClaim(1, FakeGameServer.Token));

    private static readonly EventDto Rolled = new(4, EventDto.Rolled, 1, "Yellow rolled 2 (Horse).", null, null);
    private static readonly EventDto Moved = new(5, EventDto.Moved, 1, "Yellow's Horse moved.", 2, 3);
    private static readonly EventDto Captured =
        new(6, EventDto.Captured, 1, "Yellow took Black's Pawn (+1).", null, null);

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenItIsThePlayersTurn_ShowsTheBoardAndWhatTheyMayDo()
    {
        // Arrange - nothing beyond seat 0 to move

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (4, "Raja", "Your move.", "Black you 0", "Pass", "turn yours", true),
            (
                table.FindAll("rect.cell").Count,
                table.Find("svg.board .piece title").TextContent,
                table.Find("p.status").TextContent,
                Text(table.FindAll("li.seat")[0]),
                table.Find("button.action").TextContent.Trim(),
                table.Find("div.turn").ClassName,
                table.Find("svg.board .piece").ClassList.Contains("movable")
            )
        );
    }

    [Fact]
    public void Render_WhenACellIsAThrone_OutlinesItInItsSeatsColour()
    {
        // Arrange - nothing beyond the board, whose cell 1 is seat 1's throne

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("throne seat-1", table.Find("rect.throne").GetAttribute("class"));
    }

    [Theory]
    [InlineData(true, false, "Pick a ringed piece, then a marked square.")]
    [InlineData(false, true, "Choose what to do:")]
    [InlineData(true, true, "Move a ringed piece (pick it, then a marked square), or:")]
    public void Render_WhenItIsThePlayersTurn_SaysHowToTakeIt(bool hasMoves, bool hasOthers, string expected)
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            LegalActions =
            [
                .. hasMoves ? new[] { FakeGameServer.Forward } : [],
                .. hasOthers ? new[] { FakeGameServer.Pass } : [],
            ],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(expected, table.Find("p.hint").TextContent);
    }

    [Fact]
    public void Click_WhenAPieceIsPickedUp_MarksWhereItCanGo()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("selected", true, false, true, "mark|mark", "piece seat-0 movable picked"),
            (
                Cell(table, 0).ClassList.Contains("selected") ? "selected" : "not selected",
                Cell(table, 1).ClassList.Contains("target"),
                Cell(table, 2).ClassList.Contains("target"),
                Cell(table, 3).ClassList.Contains("target"),
                string.Join("|", table.FindAll("circle.mark").Select(mark => mark.GetAttribute("class"))),
                table.Find("svg.board .piece").GetAttribute("class")
            )
        );
    }

    [Fact]
    public void Click_WhenThePiecePickedUpCanTakeAnother_RingsThatOne()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(1, "pawn", "Pawn", 1, 3)],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            "mark|mark capture",
            string.Join("|", table.FindAll("circle.mark").Select(mark => mark.GetAttribute("class")))
        );
    }

    [Fact]
    public void Click_WhenThePieceIsPutDownWhereItCanGo_SubmitsThatMove()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        Cell(table, 0).Click();

        // Act
        Cell(table, 1).Click();

        // Assert
        Assert.Equal(
            (FakeGameServer.GameId, FakeGameServer.Token, FakeGameServer.Forward),
            this._page.Server.Submitted
        );
    }

    [Fact]
    public void Click_WhenJustWatching_DoesNothing()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(Seating.Watching(FakeGameServer.GameId));

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            (null, "Black (human) to move.", "Black human 0", "Yellow Random Boat 5", "turn"),
            (
                this._page.Server.Submitted,
                table.Find("p.status").TextContent,
                Text(table.FindAll("li.seat")[0]),
                Text(table.FindAll("li.seat")[1]),
                table.Find("div.turn").ClassName
            )
        );
    }

    [Fact]
    public void ActionButton_WhenClicked_SubmitsTheAction()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.action").Click();

        // Assert
        Assert.Equal(FakeGameServer.Pass, this._page.Server.Submitted?.Action);
    }

    [Fact]
    public void ActionButton_WhenTheServerRefuses_SaysWhy()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Illegal, "Not now.");

        // Act
        table.Find("button.action").Click();

        // Assert
        Assert.Equal("Not now.", table.Find("p.problem").TextContent);
    }

    [Fact]
    public void Render_WhenAnotherSeatIsDeciding_WaitsWithoutOfferingActions()
    {
        // Arrange - nothing beyond seat 0 to move

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(
            ("Black (human) to move.", 0, false),
            (
                table.Find("p.status").TextContent,
                table.FindAll("button.action").Count,
                Cell(table, 0).ClassList.Contains("origin")
            )
        );
    }

    [Fact]
    public void Render_WhenTheGameCannotBeLoaded_SaysWhy()
    {
        // Arrange
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Illegal, "Not now.");

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Not now.", 0, 0),
            (table.Find("p.problem").TextContent, table.FindAll("svg").Count, table.FindAll("p.status").Count)
        );
    }

    [Fact]
    public void Render_WhenTheServerHasNoSuchGame_SaysItIsGone()
    {
        // Arrange
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.NotFound, "There is no such game.");

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("This game is gone: the server no longer has it, perhaps because it was restarted.", 0),
            (table.Find(".gone p.problem").TextContent.Trim(), table.FindAll("svg").Count)
        );
    }

    [Fact]
    public void ActionButton_WhenTheGameIsGoneMeanwhile_PutsTheBoardAwayAndStopsAsking()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.NotFound, "There is no such game.");

        // Act
        table.Find("button.action").Click();

        // Assert
        Assert.Equal(
            (1, 0, 0, 2),
            (
                table.FindAll(".gone").Count,
                table.FindAll("svg").Count,
                table.FindAll("p.status").Count,
                this._page.Server.Calls.Count
            )
        );
    }

    [Fact]
    public void BackToTheLobby_WhenTheGameIsGone_ReturnsToTheLobby()
    {
        // Arrange
        bool hasLeft = false;
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.NotFound, "There is no such game.");
        IRenderedComponent<Table> table = this._page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatZero).Add(x => x.OnLeave, () => hasLeft = true)
        );

        // Act
        table.Find(".gone button.back").Click();

        // Assert
        Assert.True(hasLeft);
    }

    [Fact]
    public void Render_WhenTheGameIsOver_SaysSo()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("The game is over.", table.Find("p.status").TextContent);
    }

    [Fact]
    public void Render_WhenDiceAreRolled_ShowsEachAndWhatItLetsMove()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Dice = [new DieDto(5, "Pawn or Raja"), new DieDto(2, "Horse")],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            "⚄ 5: Pawn or Raja|⚁ 2: Horse",
            string.Join("|", table.FindAll("ul.dice li").Select(die => die.TextContent))
        );
    }

    [Fact]
    public void Click_WhenOnlyTheHighlightsAreShown_ShowsEverythingAndRemembersIt()
    {
        // Arrange - nothing chosen yet, so only what scored is shown
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Log = [Rolled, Moved, Captured] };
        IRenderedComponent<Table> table = this.Render(SeatZero);
        (int, string) highlights =
            (table.FindAll("ol.events li").Count, table.Find("button.log-filter").GetAttribute("aria-pressed")!);

        // Act
        table.Find("button.log-filter").Click();

        // Assert
        Assert.Equal(((1, "true"), 3), (highlights, table.FindAll("ol.events li").Count));
        Assert.Equal(
            ["zaturnava.log", "everything"],
            this._page.JSInterop.Invocations["zaturnava.save"].Single().Arguments
        );
    }

    [Fact]
    public void Render_WhenNoDiceAreRolled_ShowsNone()
    {
        // Arrange - nothing beyond seat 0 to move, with no dice

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll("ul.dice"));
    }

    [Fact]
    public void Render_WhenThingsHaveHappened_ListsThemNewestFirstAndTintsTheLastMove()
    {
        // Arrange - the player chose to see everything
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.log").SetResult("everything");
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Log = [Rolled, Moved] };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Yellow's Horse moved.|Yellow rolled 2 (Horse).", "event moved seat-1", false, true, true),
            (
                string.Join("|", table.FindAll("ol.events li").Select(happened => happened.TextContent)),
                table.Find("ol.events li").ClassName,
                Cell(table, 1).ClassList.Contains("last"),
                Cell(table, 2).ClassList.Contains("last"),
                Cell(table, 3).ClassList.Contains("last")
            )
        );
    }

    [Fact]
    public void TurnButtons_WhenClicked_TurnTheBoardAndBackToTheSeatsOwnView()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        string home = Turn(table);

        // Act
        table.Find("button.turn-right").Click();
        string right = Turn(table);
        table.Find("button.turn-left").Click();
        table.Find("button.turn-left").Click();
        string left = Turn(table);
        table.Find("button.turn-home").Click();

        // Assert
        Assert.Equal(
            ("rotate(0 0.5 -0.5)", "rotate(90 0.5 -0.5)", "rotate(270 0.5 -0.5)", "rotate(0 0.5 -0.5)"),
            (home, right, left, Turn(table))
        );
    }

    [Fact]
    public void TurnButtons_WhenTheGameCannotBeLoaded_AreNotShown()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("Asleep.");

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll("button.turn-home"));
    }

    [Fact]
    public void TurnHome_WhenOnlyWatching_ShowsTheBoardUnturned()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(Seating.Watching(FakeGameServer.GameId));
        table.Find("button.turn-right").Click();

        // Act
        table.Find("button.turn-home").Click();

        // Assert
        Assert.Equal("rotate(0 0.5 -0.5)", Turn(table));
    }

    [Fact]
    public void Poll_WhenAPersonElsewhereIsDeciding_ShowsTheirMoves()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table =
            page.Render<Table>(parameters => parameters.Add(x => x.Seating, Seating.Watching(FakeGameServer.GameId)));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("The game is over.", table.Find("p.status").TextContent));
    }

    [Fact]
    public void Poll_WhenABotIsDeciding_AdvancesTheGame()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        FakeGameServer server = page.Server;
        server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = 1, Version = 4 };

        // Act
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Assert
        table.WaitForAssertion(() => Assert.Contains($"advance {FakeGameServer.GameId} 4", server.Calls));
    }

    [Fact]
    public async Task Poll_WhenItIsThePlayersTurn_LeavesTheGameAlone()
    {
        // Arrange
        await using PageContext page = new(TimeSpan.FromMilliseconds(10));
        _ = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(150), Xunit.TestContext.Current.CancellationToken);

        // Assert
        _ = Assert.Single(page.Server.Calls);
    }

    [Theory]
    [MemberData(nameof(Sounds), DisableDiscoveryEnumeration = true)]
    public void Sound_WhenSomethingHappensAfterwards_PlaysTheMostTellingSound(EventDto[] happened, string expected)
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatOne));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null, Log = [.. happened] };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("The game is over.", table.Find("p.status").TextContent));
        Assert.Equal(expected, string.Join(",", Played(page)));
    }

    public static TheoryData<EventDto[], string> Sounds =>
        new()
        {
            { [Rolled], "rolled" },
            { [Rolled, Moved], "moved" },
            { [Rolled, Moved, Captured], "captured" },
            { [new EventDto(4, EventDto.Passed, 1, "Black to play.", null, null)], string.Empty },
            { [Rolled, Escaped(1)], "rescued" },
            { [Rolled, Escaped(1), Escaped(0)], "alarm" },
        };

    [Fact]
    public void Sound_WhenOnlyWatchingAPieceEscape_PlaysAMove()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table =
            page.Render<Table>(parameters => parameters.Add(x => x.Seating, Seating.Watching(FakeGameServer.GameId)));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null, Log = [Escaped(0)] };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("The game is over.", table.Find("p.status").TextContent));
        Assert.Equal("moved", string.Join(",", Played(page)));
    }

    [Fact]
    public void Sound_WhenTheTurnComesRound_PlaysTheTurnSound()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = 1 };
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { Log = [Moved] };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("Your move.", table.Find("p.status").TextContent));
        Assert.Equal("turn", string.Join(",", Played(page)));
    }

    [Fact]
    public void Sound_WhenSoundIsOff_PlaysNothing()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table = page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatOne).Add(x => x.IsSoundOn, false)
        );

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null, Log = [Moved] };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("The game is over.", table.Find("p.status").TextContent));
        Assert.Empty(Played(page));
    }

    [Fact]
    public void Leave_WhenClicked_ReturnsToTheLobby()
    {
        // Arrange
        bool hasLeft = false;
        IRenderedComponent<Table> table = this._page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatZero).Add(x => x.OnLeave, () => hasLeft = true)
        );

        // Act
        table.Find("button.leave").Click();

        // Assert
        Assert.True(hasLeft);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, null)]
    public void PlayAgain_WhenTheGameIsOver_StartsItAgainWithTheSameBotsInTheSameSeats(bool isSeated, int? seat)
    {
        // Arrange - Yellow is the Random bot
        this._page.Server.View = Over() with
        {
            Seats =
            [
                .. FakeGameServer.SeatZeroToMove.Seats
                    .Select(held => held with { BotId = held.Bot is null ? null : "random" }),
            ],
        };
        Seating? seated = null;
        IRenderedComponent<Table> table = this._page.Render<Table>(parameters => parameters
            .Add(x => x.Seating, isSeated ? SeatZero : Seating.Watching(FakeGameServer.GameId))
            .Add(x => x.OnSeated, next => seated = next)
        );

        // Act
        table.Find("button.again").Click();

        // Assert
        Assert.Equal(
            ("classic", "person,random", seat),
            (
                this._page.Server.Created!.RulesetId,
                string.Join(',', this._page.Server.Created.Bots.Select(bot => bot ?? "person")),
                seated!.Claim?.Seat
            )
        );
    }

    [Fact]
    public void QuickGame_WhenTheGameIsOver_SitsAtSomeSeatWithEachBotOnceAmongTheOthers()
    {
        // Arrange
        this._page.Server.View = Over();
        Seating? seated = null;
        IRenderedComponent<Table> table = this._page.Render<Table>(parameters => parameters
            .Add(x => x.Seating, SeatZero)
            .Add(x => x.OnSeated, next => seated = next)
        );

        // Act
        table.Find("button.quick").Click();

        // Assert
        ImmutableList<string?> bots = this._page.Server.Created!.Bots;
        Assert.Equal(
            (3, true),
            (bots.OfType<string>().Distinct(StringComparer.Ordinal).Count(), bots[seated!.Claim!.Seat] is null)
        );
    }

    [Fact]
    public void Again_WhileTheGameGoesOn_IsNotOffered()
    {
        // Arrange - nothing beyond the game, still going on

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll("button.again"));
    }

    private static GameViewDto Over() =>
        FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([1], "Yellow won: only one team has pieces left."),
        };

    private static AngleSharp.Dom.IElement Cell(IRenderedComponent<Table> table, int id) =>
        table.Find($"rect[data-cell='{id}']");

    private static string Text(AngleSharp.Dom.IElement element) =>
        string.Join(' ', element.Children.Select(child => child.TextContent.Trim()).Where(text => text.Length > 0));

    private static string Turn(IRenderedComponent<Table> table) =>
        table.Find("svg.board > g").GetAttribute("transform") ?? string.Empty;

    private static IEnumerable<string> Played(PageContext page) =>
        page.JSInterop.Invocations["zaturnava.play"].Select(call => (string)call.Arguments[0]!);

    private IRenderedComponent<Table> Render(Seating seating) =>
        this._page.Render<Table>(parameters => parameters.Add(x => x.Seating, seating));

    [Fact]
    public void Status_WhenTheGameHasEnded_TellsHow()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([1], "Yellow won: only one team has pieces left."),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("Yellow won: only one team has pieces left.", table.Find("p.status").TextContent);
    }

    [Theory]
    [InlineData(0, false, "Answer Yellow's offer.")]
    [InlineData(1, false, "Random (Yellow) is answering Black's offer.")]
    [InlineData(0, true, "Your turn is over: offer prisoners back, or be done.")]
    [InlineData(1, true, "Yellow's turn is over: they may still offer prisoners back.")]
    public void Status_WhenOnlyRansomsAreBeingDealtWith_SaysSo(int deciding, bool isTurnOver, string expected)
    {
        // Arrange
        OfferDto offer = new(1 - deciding, deciding, [], []);
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = deciding,
            Offer = isTurnOver ? null : offer,
            IsTurnOver = isTurnOver,
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(expected, table.Find("p.status").TextContent);
    }

    [Theory]
    [InlineData(true, "Yellow's Raja holds your throne: you must roll, then move first; Yellow moves second.", 1)]
    [InlineData(false, "Yellow's Raja holds your throne: you move first; Yellow moves second.", 0)]
    public void Status_WhenAnEnemyLeaderHoldsTheirThrone_SaysWhoMovesAndUrgesARoll(
        bool mayRoll,
        string expected,
        int urged
    )
    {
        // Arrange - Yellow's Raja on cell 2, Black's throne
        ActionDto roll = new("roll", ImmutableSortedDictionary<string, string>.Empty.Add(ActionArguments.Actor, "0"));
        GameViewDto view = FakeGameServer.SeatZeroToMove;
        this._page.Server.View = view with
        {
            Cells = view.Cells.SetItem(2, view.Cells[2] with { Throne = 0 }),
            Pieces = [new PieceDto(0, "raja", "Raja", 1, 2)],
            LegalActions = mayRoll ? [roll] : [FakeGameServer.Pass],
            ThroneHeldBy = 1,
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (expected, urged),
            (table.Find("p.status").TextContent, table.FindAll("button.urged").Count)
        );
    }

    [Theory]
    [InlineData(0, "Your Pawn is going home: choose a marked square for it.")]
    [InlineData(1, "Random (Yellow) is choosing where their Pawn goes home.")]
    public void Status_WhenAPieceGoingHomeWaitsOnWhereItGoes_SaysWhoChooses(int deciding, string expected)
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = deciding,
            Returning = new PrisonerDto(4, "pawn", "Pawn", deciding),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(expected, table.Find("p.status").TextContent);
    }

    [Theory]
    [InlineData(0, 1, "Your move, for Yellow.")]
    [InlineData(1, 0, "Random (Yellow) to move, for Black.")]
    public void Status_WhenOneSeatMovesForAnother_SaysForWhom(int deciding, int actingFor, string expected)
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = deciding, ActingFor = actingFor };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(expected, table.Find("p.status").TextContent);
    }

    [Fact]
    public void Board_WhenDrawn_NamesEachCellAndDrawsTheRajaAndBoatAsShapes()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(1, "boat", "Boat", 1, 3)],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("c0,c1,c2,c3", 2, 0),
            (
                string.Join(",", table.FindAll("g.cell-name text").Select(name => name.TextContent)),
                table.FindAll("svg.board g.piece path").Count,
                table.FindAll("svg.board g.piece text").Count
            )
        );
    }

    [Fact]
    public void Seats_WhenSeated_GroupsThemByTeamWithItsTotalTheirOwnTeamFirst()
    {
        // Arrange - Black (0) and Green (3) against Yellow (5) and Red (2); the player is Yellow
        ImmutableList<SeatDto> seats = FakeGameServer.SeatZeroToMove.Seats;
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats =
            [
                .. seats,
                new SeatDto(2, "Green", 0, 3, "bot", "Random", []),
                new SeatDto(3, "Red", 1, 2, "bot", "Random", []),
            ],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(
            ["Yellow & Red 7 mine", "Black & Green 3 "],
            table.FindAll("section.team").Select(team =>
                $"{team.QuerySelector(".team-name")!.TextContent} {team.QuerySelector(".team-score")!.TextContent} "
                + (team.ClassList.Contains("mine") ? "mine" : string.Empty)
            )
        );
    }

    [Fact]
    public void Seats_WhenTheyHoldPrisoners_ShowEachAsItsPiece()
    {
        // Arrange
        ImmutableList<SeatDto> seats = FakeGameServer.SeatZeroToMove.Seats;
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = seats.SetItem(0, seats[0] with { Prisoners = [new PrisonerDto(9, "pawn", "Pawn", 1)] }),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("piece-icon piece seat-1", "♟︎", "piece-icon piece seat-0", 1),
            (
                table.FindAll("li.seat")[0].QuerySelector("svg")?.GetAttribute("class"),
                table.FindAll("li.seat")[0].QuerySelector("svg text")?.TextContent,
                table.FindAll("li.seat")[1].QuerySelector("svg")?.GetAttribute("class"),
                table.FindAll("li.seat")[1].QuerySelectorAll("svg path").Length
            )
        );
    }

    [Fact]
    public void Offer_WhenPrisonersAreChosenToGiveAndToAskFor_SubmitsThatOffer()
    {
        // Arrange
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();
        table.Find(".ransom-ask button.prisoner").Click();
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal(("offer", "1", "9", "7"), Offered(this._page.Server));
    }

    [Fact]
    public void Offer_WhenAllOfASeatsPrisonersAreChosen_GivesThemAllBack()
    {
        // Arrange
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();

        // Act
        table.FindAll(".ransom-to")[0].QuerySelector("button.all")!.Click();
        string label = table.Find("button.offer").TextContent.Trim();
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal(("Give them back", ("offer", "1", "9,10", string.Empty)), (label, Offered(this._page.Server)));
    }

    [Fact]
    public void Offer_WhenAnotherSeatsPrisonerIsChosen_StartsAfresh()
    {
        // Arrange
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();
        table.Find(".ransom-ask button.prisoner").Click();

        // Act
        table.FindAll(".ransom-to")[1].QuerySelector("button.all")!.Click();
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal(("offer", "3", "11", string.Empty), Offered(this._page.Server));
    }

    [Fact]
    public void Offer_WhenItsOnlyPrisonerIsChosenAgain_CannotBeMade()
    {
        // Arrange
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[1].QuerySelector("button.prisoner")!.Click();

        // Act
        table.FindAll(".ransom-to")[1].QuerySelector("button.prisoner")!.Click();

        // Assert
        Assert.True(table.Find("button.offer").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(false, "Black offers Yellow Yellow's Pawn back, as a gift.")]
    [InlineData(true, "Black offers Yellow Yellow's Pawn back, for Black's Boat.")]
    public void Render_WhenAnOfferWaitsOnItsAnswer_SaysWhatIsOfferedAndHowLongIsLeft(bool isAsking, string expected)
    {
        // Arrange
        GameViewDto ransoms = Ransoms();
        ImmutableList<PrisonerDto> asked = isAsking ? [ransoms.Seats[1].Prisoners[0]] : [];
        this._page.Server.View = ransoms with
        {
            DecidingSeat = 1,
            MayOffer = false,
            Offer = new OfferDto(0, 1, [ransoms.Seats[0].Prisoners[0]], asked),
            SecondsLeft = 30,
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (expected, "30 s left"),
            (table.Find("p.offer").TextContent, table.Find("p.countdown").TextContent)
        );
    }

    [Fact]
    public void Poll_WhenARequestFails_TriesAgain()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        FakeGameServer server = page.Server;
        server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = 1 };
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Act
        server.Failure = new HttpRequestException("Asleep.");

        // Assert
        table.WaitForAssertion(() =>
            Assert.True(server.Calls.Count(call => call.StartsWith("advance", StringComparison.Ordinal)) > 2)
        );
    }

    [Fact]
    public void Poll_WhenTimeIsUp_AdvancesTheGameForTheServerToDecide()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        FakeGameServer server = page.Server;
        server.View = FakeGameServer.SeatZeroToMove with { SecondsLeft = 0, Version = 4 };

        // Act
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Assert
        table.WaitForAssertion(() => Assert.Contains($"advance {FakeGameServer.GameId} 4", server.Calls));
    }

    [Fact]
    public async Task Poll_WhenThePlayersTimedTurnHasTimeLeft_OnlyCountsDown()
    {
        // Arrange
        await using PageContext page = new(TimeSpan.FromMilliseconds(10));
        page.Server.View = FakeGameServer.SeatZeroToMove with { SecondsLeft = 30 };
        _ = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(150), Xunit.TestContext.Current.CancellationToken);

        // Assert
        _ = Assert.Single(page.Server.Calls);
    }

    [Fact]
    public void Render_WhenAPromotionIsDue_OffersEachPieceAndToPromoteLater()
    {
        // Arrange
        this._page.Server.View = Promotions() with { PromotionDue = 2 };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (
                "Promote Black's Pawn on c2 to:",
                "Black's Boat held by you|Green's Elephant held by Yellow|Yellow's Horse held by you",
                "Promote later"
            ),
            (
                table.Find(".promotion p.hint").TextContent,
                string.Join('|', table.FindAll(".promotion .choice").Select(Choice)),
                table.Find(".promotion button.later").TextContent.Trim()
            )
        );
    }

    [Fact]
    public void Render_WhenAPawnMayBePromoted_RingsItWithoutOpeningItsMenu()
    {
        // Arrange
        this._page.Server.View = Promotions();

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (1, 0),
            (table.FindAll(".piece.promotable").Count, table.FindAll(".promotion").Count)
        );
    }

    [Fact]
    public void Render_WhenChessPiecesAreChosen_NamesAndDrawsThemSo()
    {
        // Arrange
        this._page.Server.View = Promotions() with { PromotionDue = 2 };

        // Act
        IRenderedComponent<Table> table = this._page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatZero).Add(x => x.Pieces, PieceSet.Chess)
        );

        // Assert
        Assert.Equal(
            ("Black's Boat|Green's Rook|Yellow's Knight", 1),
            (
                string.Join('|', table.FindAll(".promotion .choice .name").Select(name => name.TextContent)),
                table.FindAll(".promotion .choice path").Count
            )
        );
    }

    [Fact]
    public void Choice_WhenAPromotionIsDue_PromotesIntoTheChosenPiece()
    {
        // Arrange - the Pawn has just come to the far edge, so its choices are open
        this._page.Server.View = Promotions() with { PromotionDue = 2 };
        IRenderedComponent<Table> table = this._page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatZero).Add(x => x.Pieces, PieceSet.Chess)
        );

        // Act
        table.FindAll(".promotion button.choice")
            .First(choice => choice.TextContent.Contains("Green's Rook", StringComparison.Ordinal))
            .Click();

        // Assert
        Assert.Equal("8", this._page.Server.Submitted?.Action.Arguments[ActionArguments.Into]);
    }

    [Fact]
    public void Later_WhenAPromotionIsDue_Passes()
    {
        // Arrange
        this._page.Server.View = Promotions() with { PromotionDue = 2 };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find(".promotion button.later").Click();

        // Assert
        Assert.Equal("pass", this._page.Server.Submitted?.Action.Kind);
    }

    [Fact]
    public void Choice_WhenAPawnIsPickedUp_PromotesIt()
    {
        // Arrange
        this._page.Server.View = Promotions();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        Cell(table, 2).Click();

        // Act
        table.FindAll(".promotion button.choice")[1].Click();

        // Assert
        Assert.Equal("8", this._page.Server.Submitted?.Action.Arguments[ActionArguments.Into]);
    }

    [Fact]
    public void Later_WhenAPawnIsPickedUp_PutsItDown()
    {
        // Arrange
        this._page.Server.View = Promotions();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        Cell(table, 2).Click();

        // Act
        table.Find(".promotion button.later").Click();

        // Assert
        Assert.Equal((0, null), (table.FindAll(".promotion").Count, this._page.Server.Submitted));
    }

    [Fact]
    public void Submit_WhenThePlayerLosesDiceTheyCannotUse_SaysSoUntilDismissed()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        this._page.Server.View = Forfeited();
        table.Find("button.action").Click();
        string told = table.Find("p.notice").TextContent;

        // Act
        table.Find("p.notice button.dismiss").Click();

        // Assert
        Assert.Equal(
            (true, 0),
            (told.Contains("Black could move nothing", StringComparison.Ordinal), table.FindAll("p.notice").Count)
        );
    }

    [Fact]
    public void Poll_WhenThePlayersTurnComesRoundAgain_ForgetsTheDiceTheyLost()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(200));
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));
        page.Server.View = Forfeited();
        table.Find("button.action").Click();
        int told = table.FindAll("p.notice").Count;

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { Version = 3 };

        // Assert
        table.WaitForAssertion(() => Assert.Equal((1, 0), (told, table.FindAll("p.notice").Count)));
    }

    /// <summary>A piece of <paramref name="owner"/>'s escaping, let out by Yellow.</summary>
    private static EventDto Escaped(int owner) =>
        new(7 + owner, EventDto.Escaped, 1, "A piece escaped.", null, 9, owner);

    /// <summary>Yellow, a bot, to move, once Black has lost the dice it rolled.</summary>
    private static GameViewDto Forfeited() =>
        FakeGameServer.SeatZeroToMove with
        {
            Version = 2,
            DecidingSeat = 1,
            Log =
            [
                new EventDto(0, EventDto.Rolled, 0, "Black rolled 1 (Boat).", null, null),
                new EventDto(1, EventDto.Forfeited, 0, "Black could move nothing with 1 (Boat).", null, null),
                new EventDto(2, EventDto.Passed, 0, "Yellow to play.", null, null),
            ],
        };

    private static (string Kind, string Receiver, string Given, string Asked) Offered(FakeGameServer server)
    {
        ActionDto action = server.Submitted!.Value.Action;
        return (action.Kind, action.Arguments["receiver"], action.Arguments["given"], action.Arguments["asked"]);
    }

    /// <summary>
    /// Black's turn, with its Pawn on c2 (cell 2) able to become Green's Elephant 8, which Yellow holds,
    /// or Black's Boat 7, which Black does.
    /// </summary>
    private static GameViewDto Promotions() =>
        FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(1, "pawn", "Pawn", 0, 2)],
            Seats =
            [
                FakeGameServer.SeatZeroToMove.Seats[0] with
                {
                    Prisoners = [new PrisonerDto(7, "boat", "Boat", 0), new PrisonerDto(9, "horse", "Horse", 1)],
                },
                FakeGameServer.SeatZeroToMove.Seats[1] with
                {
                    Prisoners = [new PrisonerDto(8, "elephant", "Elephant", 2)],
                },
                new SeatDto(2, "Green", 0, 0, "open", null, []),
            ],
            LegalActions =
            [
                FakeGameServer.Pass with { Label = "Promote later" },
                Promotion(9),
                Promotion(8),
                Promotion(7),
            ],
        };

    private static string Choice(AngleSharp.Dom.IElement choice) =>
        $"{choice.QuerySelector(".name")!.TextContent} {choice.QuerySelector(".holder")!.TextContent}";

    private static ActionDto Promotion(int into) =>
        new(
            "promote",
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, "0")
                .Add(ActionArguments.Cell, "2")
                .Add(ActionArguments.Into, into.ToString(CultureInfo.InvariantCulture))
        );

    /// <summary>
    /// Black's turn, holding Yellow's Pawn 9 and Horse 10 and Red's Pawn 11; Yellow holds Black's Boat 7
    /// and Green's Elephant 8.
    /// </summary>
    private static GameViewDto Ransoms() =>
        FakeGameServer.SeatZeroToMove with
        {
            MayOffer = true,
            Seats =
            [
                new SeatDto(
                    0,
                    "Black",
                    0,
                    0,
                    "human",
                    null,
                    [
                        new PrisonerDto(9, "pawn", "Pawn", 1),
                        new PrisonerDto(10, "horse", "Horse", 1),
                        new PrisonerDto(11, "pawn", "Pawn", 3),
                    ]
                ),
                new SeatDto(
                    1,
                    "Yellow",
                    1,
                    5,
                    "bot",
                    "Random",
                    [new PrisonerDto(7, "boat", "Boat", 0), new PrisonerDto(8, "elephant", "Elephant", 2)]
                ),
                new SeatDto(2, "Green", 0, 0, "open", null, []),
                new SeatDto(3, "Red", 1, 0, "open", null, []),
            ],
        };
}
