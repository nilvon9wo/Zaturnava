using System.Collections.Immutable;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

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
            (4, "♚︎", "Your move.", "Black you 0", "Pass", "turn yours", true),
            (
                table.FindAll("rect.cell").Count,
                table.Find(".piece text").TextContent,
                table.Find("p.status").TextContent,
                Text(table.FindAll("li.seat")[0]),
                table.Find("button.action").TextContent.Trim(),
                table.Find("div.turn").ClassName,
                table.Find(".piece").ClassList.Contains("movable")
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
                table.Find(".piece").GetAttribute("class")
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
            (null, "Black to move (human).", "Black human 0", "Yellow Random Boat 5", "turn"),
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
            ("Black to move (human).", 0, false),
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
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.NotFound, "There is no such game.");

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("There is no such game.", 0, 0),
            (table.Find("p.problem").TextContent, table.FindAll("svg").Count, table.FindAll("p.status").Count)
        );
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
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Log = [Rolled, Moved] };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Yellow's Horse moved.|Yellow rolled 2 (Horse).", "event moved seat-1", false, true, true),
            (
                string.Join("|", table.FindAll("ol.log li").Select(happened => happened.TextContent)),
                table.Find("ol.log li").ClassName,
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
        };

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
    [InlineData(0, 1, "Your move, for Yellow.")]
    [InlineData(1, 0, "Yellow to move, for Black (Random).")]
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
    public void Board_WhenDrawn_NamesEachCellAndDrawsTheBoatAsAShape()
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
            ("c0,c1,c2,c3", 1, 1),
            (
                string.Join(",", table.FindAll("g.cell-name text").Select(name => name.TextContent)),
                table.FindAll("svg.board g.piece path").Count,
                table.FindAll("svg.board g.piece text").Count
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

    private static (string Kind, string Receiver, string Given, string Asked) Offered(FakeGameServer server)
    {
        ActionDto action = server.Submitted!.Value.Action;
        return (action.Kind, action.Arguments["receiver"], action.Arguments["given"], action.Arguments["asked"]);
    }

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
