using System.Collections.Immutable;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class TableTest : IDisposable
{
    private static readonly Seating SeatZero = new(FakeGameServer.GameId, new SeatClaim(0, FakeGameServer.Token));
    private static readonly Seating SeatOne = new(FakeGameServer.GameId, new SeatClaim(1, FakeGameServer.Token));

    private static readonly EventDto Rolled = new(0, EventDto.Rolled, 1, "Yellow rolled 2 (Horse).", null, null);
    private static readonly EventDto Moved = new(1, EventDto.Moved, 1, "Yellow's Horse moved.", 2, 3);
    private static readonly EventDto Captured =
        new(2, EventDto.Captured, 1, "Yellow took Black's Pawn (+1).", null, null);

    /// <summary>Splitting text into its words, however spaced.</summary>
    private const StringSplitOptions Words = StringSplitOptions.RemoveEmptyEntries;

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenTheGameEndedWithAPaidBot_WarnsThatPlayingAgainCostsTokens()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([0], "one-team-left", "only one team has pieces left"),
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                1,
                FakeGameServer.SeatZeroToMove.Seats[1] with { Bot = "Claude", BotId = "claude", IsPaidBot = true }
            ),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.again.paid").Click();

        // Assert
        Assert.Equal(
            ("Play again, with Claude: costs tokens", 0, "claude", FakeGameServer.GameId),
            (
                table.Find("button.again").TextContent.Trim(),
                table.FindAll("button.again.primary").Count,
                this._page.Server.Created?.Bots[1],
                this._page.Server.Created?.Continuing
            )
        );
    }

    [Fact]
    public void Render_WhenItIsThePlayersTurn_ShowsTheBoardAndWhatTheyMayDo()
    {
        // Arrange - nothing beyond seat 0 to move

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (4, "Raja", "Your move.", "Black You 0", "Pass", "turn yours", true),
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

    [Theory]
    [InlineData(null, "Black's Raja")]
    [InlineData("Leader", "Black's Raja, Leader")]
    public void Render_WhenACellHoldsAPiece_NamesItOnHoverOverTheCell(string? pieceClass, string expected)
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds = [new PieceKindDto("raja", "Raja", 5) { Class = pieceClass }],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal((expected, 0), (Cell(table, 0).TextContent, Cell(table, 1).ChildElementCount));
    }

    [Fact]
    public void Click_WhenOnAPieceThePlayerMayNotMove_TellsOfItBesideTheBoard()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(9, "elephant", "Elephant", 1, 3)],
            Kinds =
            [
                new PieceKindDto("elephant", "Elephant", 5)
                {
                    Moves = ["Any distance orthogonally"],
                    Captures = ["Any distance orthogonally"],
                    Powers = ["Tramples Elephants."],
                    SpecialPowers = ["Trumpets."],
                },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 3).Click();

        // Assert
        Assert.Equal(
            ("Elephant", "Owner Yellow Square c3 Value 5", "Moves Captures Common powers Special powers",
                "Tramples Elephants. Trumpets.", 0),
            (
                table.Find(".piece-card h2").TextContent,
                Facts(table),
                string.Join(" ", table.FindAll(".piece-card h3").Select(heading => heading.TextContent)),
                $"{table.Find(".piece-card ul.powers li").TextContent} "
                    + table.Find(".piece-card ul.special-powers li").TextContent,
                table.FindAll("svg.board .cell.selected").Count
            )
        );
    }

    [Fact]
    public void Click_WhenTheChessSetIsShown_TellsOfThePieceByItsNames()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5)
                {
                    Class = "Leader",
                    Restrictions = ["Never beside another Raja."],
                },
            ],
        };
        IRenderedComponent<Table> table = this._page.Render<Table>(
            parameters => parameters.Add(x => x.Seating, SeatZero).Add(x => x.Pieces, PieceSet.Chess)
        );

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("King", "Owner Black Square c0 Class Leader Value 5", "Never beside another King."),
            (
                table.Find(".piece-card h2").TextContent,
                Facts(table),
                table.Find(".piece-card ul.restrictions li").TextContent
            )
        );
    }

    [Fact]
    public void Click_WhenTheKindIsDrawn_DrawsItsMovesAndCapturesAroundThePieceInsteadOfWords()
    {
        // Arrange - on a 3 x 3 diagram, the Raja on cell 4 moves up to 7 and captures on 6 and 7
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5)
                {
                    Moves = ["One cell straight forward"],
                    Diagram = new MoveDiagramDto(3, [7], [6, 7], []),
                },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("square dark capture", "square light move capture", "square light", 1, 0, "forward moves captures"),
            (
                Square(table, 6).ClassName,
                Square(table, 7).ClassName,
                Square(table, 1).ClassName,
                Square(table, 4).QuerySelectorAll(".piece-icon").Length,
                table.FindAll(".piece-card ul.moves").Count,
                string.Join(" ", table.Find(".diagram-key").TextContent.Split((char[]?)null, Words))
            )
        );
    }

    [Fact]
    public void Click_WhenTheArmyMovesRightAndTheBoardIsTurned_TurnsTheDiagramAsTheBoardShowsIt()
    {
        // Arrange - Black's pieces move right; the board is then turned a quarter clockwise, so forward is down
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with { Forward = new DirectionDto(1, 0) }
            ),
            Kinds = [new PieceKindDto("raja", "Raja", 5) { Diagram = new MoveDiagramDto(3, [7], [7], []) }],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.turn-right").Click();

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("transform: rotate(180deg)", "transform: rotate(-180deg)", "forward moves captures"),
            (
                table.Find(".move-diagram-turn").GetAttribute("style"),
                table.Find(".move-diagram .upright").GetAttribute("style"),
                string.Join(" ", table.FindAll(".diagram-key .key-item").Select(item => item.TextContent.Trim()))
            )
        );
    }

    [Fact]
    public void Click_WhenThePieceStandsOnALightSquare_DrawsItOnALightSquareAmongDarkOnes()
    {
        // Arrange - Yellow's Elephant on c1, a light square
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(9, "elephant", "Elephant", 1, 1)],
            Kinds = [new PieceKindDto("elephant", "Elephant", 4) { Diagram = new MoveDiagramDto(3, [], [], []) }],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 1).Click();

        // Assert
        Assert.Equal(
            ("square light", "square dark", "square light"),
            (Square(table, 4).ClassName, Square(table, 1).ClassName, Square(table, 0).ClassName)
        );
    }

    [Fact]
    public void Click_WhenTheKindHopsOverAPieceDrawn_SaysWhatThatPieceIsAndTellsItsMovesInWordsToo()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5)
                {
                    Captures = ["Hops over the first piece"],
                    Diagram = new MoveDiagramDto(3, [], [], [7]),
                },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            (
                "square light screen",
                "a piece to hop over: next to it, or further along its line",
                "Hops over the first piece"
            ),
            (
                Square(table, 7).ClassName,
                table.FindAll(".diagram-key .key-item")[^1].TextContent,
                table.Find(".piece-card ul.captures li").TextContent
            )
        );
    }

    [Fact]
    public void Click_WhenARiverHoldsTheKindBack_DrawsTheRiverAlongTheFirstRowAcrossIt()
    {
        // Arrange - on a 3 x 3 diagram, a river just ahead of the Raja, under the top row
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5) { Diagram = new MoveDiagramDto(3, [1], [1], []) { RiverRow = 2 } },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("square light river-bank", "square dark", "a river it may not cross", 0),
            (
                Square(table, 7).ClassName,
                Square(table, 4).ClassName,
                table.FindAll(".diagram-key .key-item")[^1].TextContent,
                table.FindAll(".piece-card ul.moves").Count
            )
        );
    }

    [Fact]
    public void Click_WhenAPieceInTheWayWouldBlockALeap_MarksItsSquareAndSaysWhatTheMarkMeans()
    {
        // Arrange - on a 3 x 3 diagram, a piece on 7, just ahead of the Raja, would block a leap
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5) { Diagram = new MoveDiagramDto(3, [], [], []) { Blockers = [7] } },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("square light blocker", "a piece here blocks a leap"),
            (Square(table, 7).ClassName, table.FindAll(".diagram-key .key-item")[^1].TextContent)
        );
    }

    [Fact]
    public void Click_WhenTheKindLeapsToASquare_RingsItAndSaysItLeapsThereAndSlidesElsewhere()
    {
        // Arrange - on a 3 x 3 diagram, the Raja moves to 6 and 7, leaping to 6
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5) { Diagram = new MoveDiagramDto(3, [6, 7], [], []) { Leaps = [6] } },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            (
                "square dark move leap",
                "square light move",
                "leaps there, over anything between",
                "any other dot farther off: slides there, up to the first piece in the way"
            ),
            (
                Square(table, 6).ClassName,
                Square(table, 7).ClassName,
                table.FindAll(".diagram-key .key-item")[^2].TextContent,
                table.FindAll(".diagram-key .key-item")[^1].TextContent
            )
        );
    }

    [Fact]
    public void Click_WhenTheKindIsNotDrawn_TellsItsMovesInWords()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5)
                {
                    Moves = ["One cell in any direction"],
                    Diagram = new MoveDiagramDto(0, [], [], []),
                },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            ("One cell in any direction", 0),
            (table.Find(".piece-card ul.moves li").TextContent, table.FindAll(".move-diagram").Count)
        );
    }

    [Theory]
    [InlineData(false, null, "Promotes to Queen, Rook (not yet in play)")]
    [InlineData(true, "on the last row", "Promotes to Queen, Rook, on the last row")]
    public void Click_WhenTheKindHasAPromotionOfItsOwn_TellsWhatItMayBecomeWhereAndWhetherItIsPlayed(
        bool isPlayed,
        string? where,
        string expected
    )
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Kinds =
            [
                new PieceKindDto("raja", "Raja", 5)
                {
                    PromotesTo = ["Queen", "Rook"],
                    IsPromotionPlayed = isPlayed,
                    PromotesWhere = where,
                },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.EndsWith(expected, string.Join(" ", Facts(table).Split((char[]?)null, Words)), StringComparison.Ordinal);
    }

    [Fact]
    public void Click_WhenOnAnEmptyCellAfterAPiece_KeepsTellingOfThePiece()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        Cell(table, 0).Click();

        // Act
        Cell(table, 2).Click();

        // Assert
        Assert.Equal(("Raja", "Owner Black Square c0"), (table.Find(".piece-card h2").TextContent, Facts(table)));
    }

    [Fact]
    public void PutAway_WhenThePieceCardIsClosed_TellsOfNoPiece()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        Cell(table, 0).Click();

        // Act
        table.Find(".piece-card button.close").Click();

        // Assert
        Assert.Empty(table.FindAll(".piece-card"));
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

    [Fact]
    public void Render_WhenTheBoardHasARiver_DrawsItWavingWithNoWordsForIt()
    {
        // Arrange - a river between the first two cells, up the board, a cell long: four bends
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Lines = [new LineDto("river", 0.5, -0.5, 0.5, 0.5)],
            Terrain = "A river runs north to south.",
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        AngleSharp.Dom.IElement line = table.Find("path.line");
        Assert.Equal(
            (
                "line river",
                "M0.5 0.5Q0.62 0.375 0.5 0.25Q0.38 0.125 0.5 0Q0.62 -0.125 0.5 -0.25Q0.38 -0.375 0.5 -0.5",
                false
            ),
            (
                line.GetAttribute("class"),
                line.GetAttribute("d"),
                table.Markup.Contains("A river runs", StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public void Render_WhenTheBoardHasALineOfAnotherKind_DrawsItStraight()
    {
        // Arrange - a line along the top of the first cell
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Lines = [new LineDto("wall", -0.5, 1, 0.5, 1)],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("M-0.5 -1L0.5 -1", table.Find("path.line.wall").GetAttribute("d"));
    }

    [Fact]
    public void Render_WhenTheBoardHasNoTerrain_DrawsNoLines()
    {
        // Arrange - nothing beyond the board

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll("path.line"));
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

    [Theory]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 1)]
    public void Click_WhenThePiecePickedUpCouldTakeAPieceOfTheTeam_OffersItOnlyWithTheSafetyOff(
        bool isSafetyOn,
        bool isOffered,
        int warnings
    )
    {
        // Arrange - Green's Pawn, Black's ally, on cell 1; Yellow's on cell 3
        GameViewDto view = FakeGameServer.SeatZeroToMove;
        this._page.Server.View = view with
        {
            Pieces = [.. view.Pieces, new PieceDto(1, "pawn", "Pawn", 2, 1), new PieceDto(2, "pawn", "Pawn", 1, 3)],
            Seats = [.. view.Seats, new SeatDto(2, "Green", 0, 0, "bot", "Midas", [])],
        };
        IRenderedComponent<Table> table = this._page.Render<Table>(parameters =>
            parameters.Add(x => x.Seating, SeatZero).Add(x => x.IsSafetyOn, isSafetyOn)
        );

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            (isOffered, true, warnings),
            (
                Cell(table, 1).ClassList.Contains("target"),
                Cell(table, 3).ClassList.Contains("target"),
                table.FindAll("p.safety-off").Count
            )
        );
    }

    [Fact]
    public void Click_WhenTheSafetyIsTurnedOffDuringTheTurn_OffersTakingThePiecesOfTheTeam()
    {
        // Arrange - Green's Pawn, Black's ally, on cell 1
        GameViewDto view = FakeGameServer.SeatZeroToMove;
        this._page.Server.View = view with
        {
            Pieces = [.. view.Pieces, new PieceDto(1, "pawn", "Pawn", 2, 1)],
            Seats = [.. view.Seats, new SeatDto(2, "Green", 0, 0, "bot", "Midas", [])],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Render(parameters => parameters.Add(x => x.IsSafetyOn, false));
        Cell(table, 0).Click();

        // Assert
        Assert.True(Cell(table, 1).ClassList.Contains("target"));
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
    public void Click_WhenThePiecePickedUpCanSwapWithAnother_DashesItsRing()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(1, "pawn", "Pawn", 1, 3)],
            LegalActions =
            [
                FakeGameServer.Pass,
                FakeGameServer.Forward,
                FakeGameServer.Leap with { Label = "Swap places with Yellow's Pawn" },
            ],
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 0).Click();

        // Assert
        Assert.Equal(
            "mark|mark swap",
            string.Join("|", table.FindAll("circle.mark").Select(mark => mark.GetAttribute("class")))
        );
    }

    [Fact]
    public void Choose_WhenAPrisonerMayEscape_OffersItOnceAndBlinksItsSquaresRingingAPieceItWouldTake()
    {
        // Arrange - Black's Boat, held by Yellow, may escape to cell 2, or to 3, taking Yellow's Pawn there
        this.Escapable();
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.choose").Click();

        // Assert
        const string hint =
            "Now pick its destination: click a blinking circle to bring it back there, or any other square to cancel.";
        Assert.Equal(
            (1, "true", false, true, true, "mark placing|mark capture placing", hint),
            (
                table.FindAll("button.choose").Count,
                table.Find("button.choose").GetAttribute("aria-pressed"),
                Cell(table, 1).ClassList.Contains("target"),
                Cell(table, 2).ClassList.Contains("target"),
                Cell(table, 3).ClassList.Contains("target"),
                string.Join("|", table.FindAll("circle.mark").Select(mark => mark.GetAttribute("class"))),
                table.Find("p.hint").TextContent
            )
        );
    }

    [Fact]
    public void Click_WhenAMarkedSquareIsPickedForAPrisoner_SubmitsItsEscapeThere()
    {
        // Arrange
        this.Escapable();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.choose").Click();

        // Act
        Cell(table, 3).Click();

        // Assert
        (string GameId, string Token, ActionDto Action)? submitted = this._page.Server.Submitted;
        Assert.Equal(
            (FakeGameServer.GameId, FakeGameServer.Token, FakeGameServer.Escape(3).Label),
            (submitted?.GameId, submitted?.Token, submitted?.Action.Label)
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Click_WhenAnythingElseIsPickedForAPrisoner_LeavesItAPrisoner(bool isButton)
    {
        // Arrange
        this.Escapable();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.choose").Click();

        // Act
        if (isButton)
        {
            table.Find("button.choose").Click();
        }
        else
        {
            Cell(table, 1).Click();
        }

        // Assert
        Assert.Equal(
            (null, "false", 0),
            (
                this._page.Server.Submitted,
                table.Find("button.choose").GetAttribute("aria-pressed"),
                table.FindAll("circle.mark").Count
            )
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
            (null, "Black to move.", "Black human 0", "Yellow Random Boat 5", "turn"),
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
    public void Render_WhenWatchingAPlayerWhoSignedIn_CallsThemByName()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with { Player = "Ada" }
            ),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(Seating.Watching(FakeGameServer.GameId));

        // Assert
        Assert.Equal(
            ("Ada (Black) to move.", "Black Ada 0"),
            (table.Find("p.status").TextContent, Text(table.FindAll("li.seat")[0]))
        );
    }

    [Fact]
    public void Render_WhenSeatsAreStillOpen_WaitsForThemToBeTaken()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats =
            [
                .. FakeGameServer.SeatZeroToMove.Seats.SetItem(
                    1,
                    FakeGameServer.SeatZeroToMove.Seats[1] with { Controller = "open", Bot = null, BotId = null }
                ),
                new SeatDto(2, "Green", 0, 0, "open", null, []),
            ],
            LegalActions = [],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (
                "Waiting for Yellow and Green to join: the game begins once every seat is taken.",
                SeatZero.GameId,
                DocsLinks.Bots
            ),
            (
                table.Find("p.status").TextContent,
                table.Find("p.ai-join code.game-id").TextContent,
                table.Find("p.ai-join a").GetAttribute("href")
            )
        );
    }

    [Theory]
    [InlineData(0, 0.0, 60.0, 1, "")]
    [InlineData(0, 44.2, 104.2, 0, "If no one comes, you may fill the open seats with bots in 45 s.")]
    [InlineData(2, 0.0, 60.0, 0, "If no one comes, you may fill the open seats with bots in 60 s.")]
    [InlineData(2, 0.0, 0.0, 1, "")]
    public void Render_WhenSeatsAreStillOpen_OffersToFillThemWithBotsOnceThePlayerMay(
        int firstArrival,
        double untilFirst,
        double untilAnyone,
        int buttons,
        string hint
    )
    {
        // Arrange
        this._page.Server.View = WaitingForYellow() with
        {
            Vacancy = new VacancyDto(firstArrival, untilFirst, untilAnyone),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (buttons, hint),
            (
                table.FindAll("button.fill").Count,
                string.Concat(table.FindAll("p.fill-wait").Select(wait => wait.TextContent))
            )
        );
    }

    [Fact]
    public void Fill_WhenClicked_HasBotsTakeTheOpenSeats()
    {
        // Arrange
        this._page.Server.View = WaitingForYellow() with { Vacancy = new VacancyDto(0, 0, 60) };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.fill").Click();

        // Assert
        Assert.Contains($"fill {FakeGameServer.GameId} {FakeGameServer.Token}", this._page.Server.Calls);
    }

    [Fact]
    public void Render_WhenWatchingAGameWaitingForItsSeats_OffersNoWayToFillThem()
    {
        // Arrange
        this._page.Server.View = WaitingForYellow() with { Vacancy = new VacancyDto(0, 0, 0) };

        // Act
        IRenderedComponent<Table> table = this.Render(Seating.Watching(FakeGameServer.GameId));

        // Assert
        Assert.Equal((0, 0), (table.FindAll("button.fill").Count, table.FindAll("p.fill-wait").Count));
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
            ("Black to move.", 0, false),
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
    public void Render_WhenTheGamesRulesHaveChangedSince_SaysSoAndNothingElse()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Gone, "Rules changed: start a new one.");

        // Act
        table.Find("button.action").Click();

        // Assert
        Assert.Equal(
            ("Rules changed: start a new one.", 0, 0),
            (
                table.Find(".gone p.problem").TextContent.Trim(),
                table.FindAll("p.status").Count,
                table.FindAll(".turn").Count
            )
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
    public void Render_WhenDoublesMoveNothingButLetACapturedPieceEscape_GreysThemOutAndSaysSo()
    {
        // Arrange - double 5 moving nothing, and seat 0's captured Raja may escape home
        ActionDto escape = new(
            "escape",
            ImmutableSortedDictionary<string, string>.Empty.Add("actor", "0").Add("piece", "3").Add("home", "3")
        );
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Dice = [new DieDto(5, "Pawn or Raja", IsUsable: false), new DieDto(5, "Pawn or Raja", IsUsable: false)],
            LegalActions = [FakeGameServer.Pass, escape],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.All(
            table.FindAll("ul.dice li"),
            die => Assert.Equal(
                ("unusable", "Nothing it lets move can move now, but the doubles let a captured piece escape"),
                (die.ClassName, die.GetAttribute("title"))
            )
        );
    }

    [Fact]
    public void Render_WhenADieLetsNothingMove_GreysItOut()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Dice = [new DieDto(5, "Pawn or Raja"), new DieDto(2, "Horse", IsUsable: false)],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            [("", false), ("unusable", true)],
            table.FindAll("ul.dice li").Select(die => (die.ClassName ?? "", die.HasAttribute("title")))
        );
    }

    [Theory]
    [InlineData("everything", 3)]
    [InlineData("highlights", 0)]
    public void Render_WhenOlderEventsWereLetGo_SaysSoWhereTheyWere(string shown, int expected)
    {
        // Arrange - events 3 and 4 were let go; 2, a capture, was kept
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.log").SetResult(shown);
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Log = [Captured, Moved with { Number = 5 }, Captured with { Number = 6 }],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            expected,
            table.FindAll("ol.events li").Select(item => item.ClassName).ToList().IndexOf("dropped") + 1
        );
    }

    [Fact]
    public void Render_WhenNothingWasLetGo_SaysNothingOfOlderEvents()
    {
        // Arrange - from the very first event
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.log").SetResult("everything");
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Log = [Rolled, Moved] };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll("ol.events li.dropped"));
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
    public void Poll_WhenItIsThePlayersUntimedTurn_LooksAgainKeepingThePiecePickedUp()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));
        FakeGameServer server = page.Server;

        // Act - a poll may redraw the board between finding the cell and clicking it; then click again
        table.WaitForAssertion(() => Cell(table, 0).Click());

        // Assert
        table.WaitForAssertion(() => Assert.True(server.Calls.Count > 2));
        Assert.True(Cell(table, 0).ClassList.Contains("selected"));
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
            { [Rolled, RajaTaken(0)], "triumph" },
            { [Rolled, Escaped(1), RajaTaken(0)], "triumph" },
            { [Escaped(0), RajaTaken(0)], "alarm" },
            { [Escaped(0), RajaTaken(1)], "lament" },
            { [RajaBack(EventDto.Escaped, 1)], "restored" },
            { [RajaBack(EventDto.Escaped, 0)], "dread" },
            { [RajaBack(EventDto.Ransom, 0), RajaTaken(0)], "dread" },
            { [Power(EventDto.Command, 1, isOver: false)], "rally" },
            { [Power(EventDto.Command, 0, isOver: false)], "menace" },
            { [Power(EventDto.Command, 0, isOver: true)], "rally" },
            { [Power(EventDto.Command, 1, isOver: true)], "menace" },
            { [Power(EventDto.Empire, 1, isOver: false)], "glory" },
            { [Power(EventDto.Empire, 0, isOver: false)], "doom" },
            { [Power(EventDto.Command, 1, isOver: false), Power(EventDto.Empire, 0, isOver: false)], "doom" },
            { [RajaTaken(0), Power(EventDto.Command, 0, isOver: false)], "triumph,menace" },
            { [new EventDto(40, EventDto.Captured, 0, "Black took Yellow's Pawn.", null, null, 1)], "lost" },
            { [new EventDto(40, EventDto.Captured, 0, "Black took Yellow's Pawn.", null, null, 1), Moved], "lost" },
        };

    [Theory]
    [InlineData(new[] { 1 }, "victory")]
    [InlineData(new[] { 0 }, "defeat")]
    [InlineData(new int[0], "")]
    public void Sound_WhenTheGameEnds_PlaysVictoryOrDefeat(int[] winners, string expected)
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatOne));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([.. winners], "over", "over"),
            Log = [new EventDto(50, EventDto.Ended, 0, "Over.", null, null)],
        };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("over", table.Find("p.status").TextContent));
        Assert.Equal(expected, string.Join(",", Played(page)));
    }

    [Theory]
    [InlineData(1, new[] { 1 }, "victory", "Victory!")]
    [InlineData(0, new[] { 1 }, "defeat", "Defeat")]
    [InlineData(0, new int[0], "draw", "A draw")]
    [InlineData(-1, new[] { 1 }, "watched", "Yellow won")]
    public void Render_WhenTheGameIsOver_SaysHowItEndedAndMarksTheWinnersPoints(
        int seat,
        int[] winners,
        string result,
        string verdict
    )
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([.. winners], "agreement", "points decide"),
        };
        Seating seating = seat < 0
            ? Seating.Watching(FakeGameServer.GameId)
            : new Seating(FakeGameServer.GameId, new SeatClaim(seat, FakeGameServer.Token));

        // Act
        IRenderedComponent<Table> table = this.Render(seating);

        // Assert
        Assert.Equal(
            (true, verdict, winners.Length, winners.Length),
            (
                table.Find(".result").ClassList.Contains(result),
                table.Find(".result .verdict").TextContent,
                table.FindAll(".result .totals li.won .badge").Count,
                table.FindAll("section.team.won").Count
            )
        );
    }

    [Fact]
    public void Render_WhilePlaying_ShowsNoResult()
    {
        // Arrange - nothing beyond seat 0 to move

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal((0, 0), (table.FindAll(".result").Count, table.FindAll("section.team.won").Count));
    }

    [Fact]
    public void Sound_WhenOnlyWatchingAPieceEscapeAndARajaTaken_PlaysACapture()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table =
            page.Render<Table>(parameters => parameters.Add(x => x.Seating, Seating.Watching(FakeGameServer.GameId)));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with { DecidingSeat = null, Log = [Escaped(0), RajaTaken(1)] };

        // Assert
        table.WaitForAssertion(() => Assert.Equal("The game is over.", table.Find("p.status").TextContent));
        Assert.Equal("captured", string.Join(",", Played(page)));
    }

    [Fact]
    public void Sound_WhenOnlyWatchingARajaComeBackAndCommandChange_PlaysAMove()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<Table> table =
            page.Render<Table>(parameters => parameters.Add(x => x.Seating, Seating.Watching(FakeGameServer.GameId)));

        // Act
        page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Log = [RajaBack(EventDto.Escaped, 0), Power(EventDto.Command, 0, isOver: false)],
        };

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
            ("chaturaji-2027", "person,random", seat),
            (
                this._page.Server.Created!.RulesetId,
                string.Join(',', this._page.Server.Created.Bots.Select(bot => bot ?? "person")),
                seated!.Claim?.Seat
            )
        );
    }

    [Fact]
    public void PlayAgain_WhenTheDealtGameIsOverAndTheSameArmiesAreChosen_DealsThemAgainAlikeAsBefore()
    {
        // Arrange - a dealt game, its armies alike
        this._page.Server.View = Over() with { IsDealt = true, AreArmiesAlike = true };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.same-armies").Click();

        // Assert
        Assert.Equal(
            ("Play again, same armies", true, FakeGameServer.GameId),
            (
                table.Find("button.same-armies").TextContent.Trim(),
                this._page.Server.Created!.AreArmiesAlike,
                this._page.Server.Created.ArmiesOf
            )
        );
    }

    [Fact]
    public void PlayAgain_WhenTheArmiesWereDrafted_HasThemDraftedAgain()
    {
        // Arrange
        this._page.Server.View = Over() with { IsDealt = true, IsDrafted = true };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.same-armies").Click();

        // Assert
        Assert.True(this._page.Server.Created!.IsDrafted);
    }

    [Fact]
    public void PlayAgain_WhenTheBoardWasSet_PlaysOnTheSameBoard()
    {
        // Arrange
        this._page.Server.View = Over() with { IsDealt = true, Board = new BoardSizeDto(12, 4, 4) };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.same-armies").Click();

        // Assert
        Assert.Equal(new BoardSizeDto(12, 4, 4), this._page.Server.Created!.Board);
    }

    [Fact]
    public void Render_WhileTheArmiesAreDrafted_TellsWhoDraftsAndThatTheScoresArePointsLeftToSpend()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { DraftLeft = [10, 20], DecidingSeat = 1 };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Random (Yellow) is drafting their army.", "Points left to spend"),
            (table.Find("p.status").TextContent, table.Find("span.score").GetAttribute("title"))
        );
    }

    [Fact]
    public void Click_WhenTheThroneIsToBeSet_SetsItOnTheSquareClickedWithoutAChoiceFirst()
    {
        // Arrange - the throne may go on cell 1 or 2
        this.Drafting(left: 3, throne: true);
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        Cell(table, 2).Click();

        // Assert
        Assert.Equal(
            ("Your turn to draft your army.", true, "throne 2"),
            (
                table.Find("p.status").TextContent,
                table.FindAll("p.draft-throne").Count == 1,
                $"{this._page.Server.Submitted?.Action.Kind} {this._page.Server.Submitted?.Action.Arguments["home"]}"
            )
        );
    }

    [Fact]
    public void Change_WhenThePlayerDraftingAsksForEveryPiece_ListsThemAllGreyingWhatTheyCannotAfford()
    {
        // Arrange - 3 left: the Raja, at 5, is too dear, and so at first not listed
        this.Drafting(left: 3, throne: false);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        string affordable = string.Join("|", table.FindAll(".draft-table .name").Select(name => name.TextContent));

        // Act
        table.Find("input.draft-affordable").Change(false);

        // Assert
        Assert.Equal(
            (
                "Boat|Pawn",
                "Raja|Boat|Pawn",
                "unaffordable",
                true,
                "Moves forward; captures: diagonally forward",
                "Pick a piece above, then a marked square of your kingdom for it."
            ),
            (
                affordable,
                string.Join("|", table.FindAll(".draft-table .name").Select(name => name.TextContent)),
                table.FindAll(".draft-table tbody tr")[0].GetAttribute("class"),
                table.FindAll("button.pick")[0].HasAttribute("disabled"),
                table.FindAll(".draft-table td.moves")[2].TextContent,
                table.Find("p.draft-place").TextContent.Trim()
            )
        );
    }

    [Theory]
    [InlineData("Cavalry", "", false, "Boat", "draft-kin")]
    [InlineData("", "Forward", false, "Pawn", "draft-bearing")]
    [InlineData("", "", true, "Boat|Pawn", "draft-bearing")]
    public void Change_WhenTheDraftIsFiltered_ListsOnlyThePiecesLetThrough(
        string pieceClass,
        string bearing,
        bool isAffordableOnly,
        string expected,
        string subFilter
    )
    {
        // Arrange
        this.Drafting(left: 3, throne: false);
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("select.draft-class").Change(pieceClass);
        if (bearing.Length > 0)
        {
            table.Find("select.draft-bearing").Change(bearing);
        }

        table.Find("input.draft-affordable").Change(isAffordableOnly);

        // Assert
        Assert.Equal(
            (expected, 1),
            (
                string.Join("|", table.FindAll(".draft-table .name").Select(name => name.TextContent)),
                table.FindAll($"select.{subFilter}").Count
            )
        );
    }

    [Theory]
    [InlineData("Cavalry", 1)]
    [InlineData("Infantry", 0)]
    public void Change_WhenTheFiltersHideThePiecePickedToDraft_LetsItGo(string pieceClass, int picked)
    {
        // Arrange - the Boat, Cavalry, picked
        this.Drafting(left: 3, throne: false);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll("button.pick")[0].Click();

        // Act
        table.Find("select.draft-class").Change(pieceClass);

        // Assert
        Assert.Equal(
            (picked, picked),
            (table.FindAll("button.pick[aria-pressed=true]").Count, table.FindAll("aside.piece-card").Count)
        );
    }

    [Fact]
    public async Task Poll_WhenThePlayersTurnToDraftComesRoundAgain_KeepsTheFiltersAsTheyLeftThem()
    {
        // Arrange - the player lists Infantry only, then another seat drafts; the filter is found and changed in one
        // go, so that a poll cannot render the table anew in between
        await using PageContext page = new(TimeSpan.FromMilliseconds(10));
        page.Server.View = DraftingView(left: 3, throne: false);
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));
        await table.InvokeAsync(
            () => table.Find("select.draft-class").ChangeAsync(new ChangeEventArgs { Value = "Infantry" })
        );
        page.Server.View = page.Server.View with { DecidingSeat = 1, Version = 2 };
        await table.WaitForAssertionAsync(
            () => Assert.Empty(table.FindAll("select.draft-class")),
            TimeSpan.FromSeconds(10)
        );

        // Act
        page.Server.View = DraftingView(left: 3, throne: false) with { Version = 3 };

        // Assert
        await table.WaitForAssertionAsync(
            () => Assert.Equal(
                ("Infantry", "Pawn"),
                (
                    table.Find("select.draft-class").GetAttribute("value"),
                    string.Join("|", table.FindAll(".draft-table .name").Select(name => name.TextContent))
                )
            ),
            TimeSpan.FromSeconds(10)
        );
    }

    [Fact]
    public void Click_WhenAPiecePickedToDraftIsPlaced_DraftsItThereHavingShownItsCard()
    {
        // Arrange
        this.Drafting(left: 3, throne: false);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll("button.pick")[0].Click();
        string card = $"{table.Find("aside.piece-card h2").TextContent} from {table.Find("dd.origin").TextContent}";
        string hint = table.Find("p.draft-place").TextContent.Trim();

        // Act
        Cell(table, 1).Click();

        // Assert
        Assert.Equal(
            ("Boat from Chaturaji", "Now click a marked square of your kingdom for it.", "draft boat 1"),
            (
                card,
                hint,
                $"{this._page.Server.Submitted?.Action.Kind} {this._page.Server.Submitted?.Action.Arguments["piece"]} "
                + this._page.Server.Submitted?.Action.Arguments["home"]
            )
        );
    }

    [Fact]
    public void Click_WhenThePickedPieceIsPickedAgainOrItsCardClosed_LetsItGoOrHidesTheCard()
    {
        // Arrange
        this.Drafting(left: 3, throne: false);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll("button.pick")[0].Click();
        table.Find("aside.piece-card button.close").Click();
        int cardsClosed = table.FindAll("aside.piece-card").Count;

        // Act
        table.FindAll("button.pick")[0].Click();

        // Assert
        Assert.Equal((0, false), (cardsClosed, Cell(table, 1).ClassList.Contains("target")));
    }

    [Fact]
    public void Choose_WhenAPrisonerIsChosenToEscape_ShowsItsCard()
    {
        // Arrange
        this.Escapable();
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.choose").Click();

        // Assert
        Assert.Equal(("Boat", 0), (table.Find("aside.piece-card h2").TextContent, table.FindAll("dd.square").Count));
    }

    [Fact]
    public void Render_WhenThePlayersPieceIsGoingHome_ShowsItsCard()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Returning = new PrisonerDto(7, "boat", "Boat", 0),
            LegalActions = [],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("Boat", table.Find("aside.piece-card h2").TextContent);
    }

    /// <summary>
    /// Black drafting, with <paramref name="left"/> to spend, from a Raja (5), a Boat (2) and a Pawn (1): its throne
    /// to set on cell 1 or 2, if <paramref name="throne"/>; else any it can pay for on cell 1.
    /// </summary>
    private void Drafting(int left, bool throne) => this._page.Server.View = DraftingView(left, throne);

    /// <summary>The game as <see cref="Drafting"/> sets it.</summary>
    private static GameViewDto DraftingView(int left, bool throne) =>
        FakeGameServer.SeatZeroToMove with
        {
            DraftLeft = [left, 0],
            DraftKinds =
            [
                new PieceKindDto("raja", "Raja", 5) { Class = "Leader", Bearing = "Every way", Moves = ["Steps"] },
                new PieceKindDto("boat", "Boat", 2)
                {
                    Class = "Cavalry",
                    Kin = "Horse",
                    Origin = "Chaturaji",
                    Moves = ["Leaps"],
                },
                new PieceKindDto("pawn", "Pawn", 1)
                {
                    Class = "Infantry",
                    Bearing = "Forward",
                    Moves = ["Moves forward"],
                    Captures = ["diagonally forward"],
                    Restrictions = ["Never back"],
                },
            ],
            LegalActions = throne
                ? [Placed("throne", null, 1, "Your throne"), Placed("throne", null, 2, "Your throne"), Filling]
                : [Placed("draft", "boat", 1, "Boat, 2"), Placed("draft", "pawn", 1, "Pawn, 1"), Filling],
        };

    private static readonly ActionDto Filling =
        new("fill-army", ImmutableSortedDictionary<string, string>.Empty.Add("actor", "0"))
        {
            Label = "Fill the rest of my army at random, and be done",
        };

    private static ActionDto Placed(string kind, string? piece, int home, string choice) =>
        new(
            kind,
            ImmutableSortedDictionary<string, string>.Empty
                .Add("actor", "0")
                .Add("home", home.ToString(CultureInfo.InvariantCulture))
                .AddRange(piece is null ? [] : [KeyValuePair.Create("piece", piece)])
        )
        {
            Choice = choice,
        };

    [Theory]
    [InlineData("player", null, "player ")]
    [InlineData(null, "Gus", " Gus")]
    public void TakeOver_WhenAWatcherTakesABotsSeat_SitsThemThereByTheTokenGiven(
        string? player,
        string? nickname,
        string asked
    )
    {
        // Arrange - Yellow is the Random bot; the watcher signed in, or a guest by a nickname
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult(player);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult(nickname);
        this._page.Server.View = WithRandomAtYellow(FakeGameServer.SeatZeroToMove);
        Seating? seated = null;
        IRenderedComponent<Table> table = this._page.Render<Table>(parameters => parameters
            .Add(x => x.Seating, Seating.Watching(FakeGameServer.GameId))
            .Add(x => x.OnSeated, next => seated = next)
        );

        // Act
        table.Find(".seat-1 button.take-over").Click();

        // Assert
        Assert.Equal(
            (1, new Seating(FakeGameServer.GameId, new SeatClaim(1, FakeGameServer.Token)), true),
            (
                table.FindAll("button.take-over").Count,
                seated,
                this._page.Server.Calls.Contains($"take over {FakeGameServer.GameId} 1 {asked}")
            )
        );
    }

    [Theory]
    [InlineData("a guest")]
    [InlineData("seated")]
    [InlineData("over")]
    public void Render_WhenAGuestAtAGameForPlayersOrSomeoneSeatedWatchesOrTheGameIsOver_OffersNoSeatToTakeOver(
        string who
    )
    {
        // Arrange - the game only for players signed in
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player")
            .SetResult(who == "a guest" ? null : "player");
        this._page.Server.View =
            WithRandomAtYellow(who == "over" ? Over() : FakeGameServer.SeatZeroToMove) with { IsForPlayers = true };

        // Act
        IRenderedComponent<Table> table =
            this.Render(who == "seated" ? SeatZero : Seating.Watching(FakeGameServer.GameId));

        // Assert
        Assert.Empty(table.FindAll("button.take-over"));
    }

    private static GameViewDto WithRandomAtYellow(GameViewDto view) =>
        view with
        {
            Seats = [.. view.Seats.Select(held => held with { BotId = held.Bot is null ? null : "random" })],
        };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlayAgain_WhenTheGameWasOrWasNotForPlayersOnly_IsAsItWas(bool isForPlayers)
    {
        // Arrange
        this._page.Server.View = Over() with { IsForPlayers = isForPlayers };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.again").Click();

        // Assert
        Assert.Equal(isForPlayers, this._page.Server.Created!.IsForPlayers);
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

    /// <summary>Seat 0 to move, which may propose ending the game if <paramref name="mayPropose"/>.</summary>
    private static GameViewDto Proposable(bool mayPropose) =>
        FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with { MayProposeEnd = mayPropose }
            ),
        };

    /// <summary>Seat <paramref name="deciding"/> asked whether to end the game; Yellow proposed, giving up.</summary>
    private static GameViewDto Proposed(int deciding) =>
        FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = deciding,
            Proposal = new EndProposalDto(1, Resigns: true),
            SecondsLeft = 60,
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                1,
                FakeGameServer.SeatZeroToMove.Seats[1] with { HasGivenUp = true }
            ),
            LegalActions = [Answer(deciding, agrees: true), Answer(deciding, agrees: false)],
        };

    private static ActionDto Answer(int seat, bool agrees) =>
        new(
            "answer-end",
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, seat.ToString(CultureInfo.InvariantCulture))
                .Add("agrees", agrees ? "true" : "false")
        )
        {
            Label = agrees ? "Agree to end the game" : "Keep playing",
        };

    private static GameViewDto Over() =>
        FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([1], "one-team-left", "Yellow won: only one team has pieces left."),
        };

    private static AngleSharp.Dom.IElement Cell(IRenderedComponent<Table> table, int id) =>
        table.Find($"rect[data-cell='{id}']");

    /// <summary>The piece card's labelled facts, each label and its value, as words: "Owner Black Square c0".</summary>
    private static string Facts(IRenderedComponent<Table> table) =>
        string.Join(
            " ",
            table.FindAll(".piece-card .facts dt, .piece-card .facts dd").Select(fact => fact.TextContent)
        );

    /// <summary>A square of the piece card's diagram.</summary>
    private static AngleSharp.Dom.IElement Square(IRenderedComponent<Table> table, int id) =>
        table.Find($".move-diagram .square[data-cell='{id}']");

    private static string Text(AngleSharp.Dom.IElement element) =>
        string.Join(' ', element.Children.Select(child => child.TextContent.Trim()).Where(text => text.Length > 0));

    private static string Turn(IRenderedComponent<Table> table) =>
        table.Find("svg.board > g").GetAttribute("transform") ?? string.Empty;

    private static bool IsGet(string call) => call.StartsWith("get", StringComparison.Ordinal);

    private static IEnumerable<string> Played(PageContext page) =>
        page.JSInterop.Invocations["zaturnava.play"].SelectMany(call => call.Arguments.Select(sound => (string)sound!));

    /// <summary>Black to move, whose Boat, held by Yellow, may escape to cell 2, or 3, taking Yellow's Pawn.</summary>
    private void Escapable() =>
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [.. FakeGameServer.SeatZeroToMove.Pieces, new PieceDto(1, "pawn", "Pawn", 1, 3)],
            LegalActions = [FakeGameServer.Escape(2), FakeGameServer.Escape(3), FakeGameServer.Pass],
        };

    private IRenderedComponent<Table> Render(Seating seating) =>
        this._page.Render<Table>(parameters => parameters.Add(x => x.Seating, seating));

    [Fact]
    public void Shown_WhenThePageIsShownAgain_LooksAtTheGameAtOnce()
    {
        // Arrange
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        _ = table.InvokeAsync(table.Instance.Shown);

        // Assert
        table.WaitForAssertion(() => Assert.Equal(2, this._page.Server.Calls.Count(IsGet)));
    }

    [Fact]
    public async Task Shown_WhenTheGameIsOver_LooksNoMore()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([1], "one-team-left", "Yellow won: only one team has pieces left."),
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        await table.InvokeAsync(table.Instance.Shown);
        await Task.Delay(TimeSpan.FromMilliseconds(100), Xunit.TestContext.Current.CancellationToken);

        // Assert
        _ = Assert.Single(this._page.Server.Calls);
    }

    [Fact]
    public async Task Shown_WhenTheGameCouldNotBeSeen_WaitsAsBefore()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("The server is asleep.");
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        await table.InvokeAsync(table.Instance.Shown);
        await Task.Delay(TimeSpan.FromMilliseconds(100), Xunit.TestContext.Current.CancellationToken);

        // Assert
        _ = Assert.Single(this._page.Server.Calls);
    }

    [Fact]
    public void Render_WhenShown_AsksToBeToldWhenThePageIsShownAgain()
    {
        // Arrange - nothing beyond the game

        // Act
        _ = this.Render(SeatZero);

        // Assert
        _ = Assert.Single(this._page.JSInterop.Invocations["zaturnava.watch"]);
    }

    [Fact]
    public void Status_WhenTheGameHasEnded_TellsHow()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = null,
            Outcome = new OutcomeDto([1], "one-team-left", "Yellow won: only one team has pieces left."),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal("Yellow won: only one team has pieces left.", table.Find("p.status").TextContent);
    }

    [Theory]
    [InlineData(0, false, "Answer Random (Yellow)'s offer.")]
    [InlineData(1, false, "Random (Yellow) is answering Black's offer.")]
    [InlineData(0, true, "Your turn is over: offer prisoners back, or be done.")]
    [InlineData(1, true, "Random (Yellow)'s turn is over: they may still offer prisoners back.")]
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
    [InlineData(
        true,
        "Yellow's Raja holds your throne, so you cannot move before rolling: you must roll, then move first; "
            + "Yellow moves second.",
        1
    )]
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

    [Fact]
    public void Status_WhenAnEnemyLeaderHoldsTheThroneOfTheAllyThePlayerActsFor_SaysWhose()
    {
        // Arrange - Black plays Green's turn; Yellow's Raja on cell 2, Green's throne
        ActionDto roll = new("roll", ImmutableSortedDictionary<string, string>.Empty.Add(ActionArguments.Actor, "0"));
        GameViewDto view = FakeGameServer.SeatZeroToMove;
        this._page.Server.View = view with
        {
            ActingFor = 2,
            Cells = view.Cells.SetItem(2, view.Cells[2] with { Throne = 2 }),
            Pieces = [new PieceDto(0, "raja", "Raja", 1, 2)],
            Seats = [.. view.Seats, new SeatDto(2, "Green", 0, 0, "bot", "Midas", [])],
            LegalActions = [roll],
            ThroneHeldBy = 1,
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.StartsWith(
            "Yellow's Raja holds Green's throne",
            table.Find("p.status").TextContent,
            StringComparison.Ordinal
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
    public void Board_WhenPiecesAreMarkedBySymbol_DrawsEachWithItsClassAndLettersBeneath()
    {
        // Arrange - a Camel, its own shape; a Xiangqi Cannon sharing its; a Dragon King, a glyph with wings round it;
        // a Pawn of no class told
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces =
            [
                new PieceDto(1, "camel", "Camel", 1, 0) { Mark = Marked("camel", string.Empty, "cavalry") },
                new PieceDto(2, "xiangqi-cannon", "Cannon", 1, 1) { Mark = Marked("cannon", "X", "artillery") },
                new PieceDto(3, "dragon-king", "Dragon King", 0, 2) { Mark = Marked("dragon-king", "DK", "leader") },
                new PieceDto(4, "western-pawn", "Pawn (Western)", 0, 3) { Mark = new PieceMarkDto("pawn", "W") },
            ],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        ImmutableList<string> classes =
        [
            .. table.FindAll("svg.board g.piece path.class-emblem").Select(path => path.GetAttribute("transform")!),
        ];
        Assert.Equal(
            ("X,DK,W", 2, 6, 3, true, true),
            (
                string.Join(",", table.FindAll("svg.board g.piece text.letters").Select(text => text.TextContent)),
                table.FindAll("svg.board g.piece text.symbol").Count,
                table.FindAll("svg.board g.piece path").Count,
                classes.Count,
                classes[0].EndsWith(PieceStyle.ClassPlacementBeside(string.Empty), StringComparison.Ordinal),
                classes[1].EndsWith(PieceStyle.ClassPlacementBeside("X"), StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public void Board_WhenAPieceIsMarkedBySymbolNotKnownHere_DrawsNoneButItsLetters()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Pieces = [new PieceDto(2, "wizard", "Wizard", 0, 2) { Mark = Marked("wizard", "Wz", "sorcery") }],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Wz", 0),
            (
                string.Join(",", table.FindAll("svg.board g.piece text").Select(text => text.TextContent)),
                table.FindAll("svg.board g.piece path").Count
            )
        );
    }

    [Fact]
    public void Seats_WhenTheyHoldPrisonersMarkedBySymbol_ShowEachByItsMark()
    {
        // Arrange
        ImmutableList<SeatDto> seats = FakeGameServer.SeatZeroToMove.Seats;
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = seats.SetItem(
                0,
                seats[0] with
                {
                    Prisoners =
                    [
                        new PrisonerDto(9, "camel", "Camel", 1) { Mark = Marked("camel", "", "cavalry") },
                        new PrisonerDto(8, "chancellor", "Chancellor", 1) { Mark = Marked("chancellor", "", "heavy") },
                        new PrisonerDto(7, "western-pawn", "Pawn", 1) { Mark = new PieceMarkDto("pawn", "W") },
                        new PrisonerDto(6, "wizard", "Wizard", 1) { Mark = Marked("wizard", "Wz", "sorcery") },
                    ],
                }
            ),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        AngleSharp.Dom.IElement held = table.FindAll("li.seat")[0];
        Assert.Equal(
            ("W,Wz", 2, 4),
            (
                string.Join(",", held.QuerySelectorAll("svg text.letters").Select(text => text.TextContent)),
                held.QuerySelectorAll("svg text.symbol").Length,
                held.QuerySelectorAll("svg path").Length
            )
        );
    }

    /// <summary>A piece marked by <paramref name="symbol"/>, any <paramref name="letters"/>, and its class.</summary>
    private static PieceMarkDto Marked(string symbol, string letters, string pieceClass) =>
        new(symbol, letters) { Class = pieceClass };

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
    public void Offer_WhenThePlayerHoldsTheirOwnPieces_ReleasesAllThatMayGoBackGreyingTheRestWithNothingToAskFor()
    {
        // Arrange - Black holds its own Boat 12, and its own Pawn 13, taken too lately to go back yet
        GameViewDto ransoms = Ransoms();
        SeatDto black = ransoms.Seats[0];
        this._page.Server.View = ransoms with
        {
            Seats = ransoms.Seats.SetItem(
                0,
                black with
                {
                    Prisoners =
                    [
                        .. black.Prisoners,
                        new PrisonerDto(12, "boat", "Boat", 0),
                        new PrisonerDto(13, "pawn", "Pawn", 0) { MayGoBack = false },
                    ],
                }
            ),
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);
        AngleSharp.Dom.IElement mine = table.FindAll(".ransom-to")[0];

        // Act
        string offered =
            $"{mine.QuerySelector(".name")!.TextContent} {mine.QuerySelectorAll("button.prisoner").Length}";
        AngleSharp.Dom.IElement waiting = mine.QuerySelector("button.prisoner.waiting")!;
        string greyed = $"{waiting.GetAttribute("title")} {waiting.HasAttribute("disabled")}";
        int hints = table.FindAll("p.under-investigation").Count;
        mine.QuerySelector("button.all")!.Click();
        int askable = table.FindAll(".ransom-ask").Count;
        table.Find("button.offer").Click();
        (string kind, string receiver, string given, _) = Offered(this._page.Server);

        // Assert
        Assert.Equal(
            ("To yourself: 2", "Pawn: loyalty under investigation True", 1, 0),
            (offered, greyed, hints, askable)
        );
        Assert.Equal(("offer", "0", "12"), (kind, receiver, given));
    }

    [Fact]
    public void Offer_WhenOfferingAnAllyItsPieces_MayAskForThoseOfTheirsGreyingThoseThatMayNotGoBack()
    {
        // Arrange - Black holds Green's Horse 12; Green holds Black's Boat 13, and its Pawn 14, taken too lately
        GameViewDto ransoms = Ransoms();
        this._page.Server.View = ransoms with
        {
            Seats = ransoms.Seats
                .SetItem(0, ransoms.Seats[0] with { Prisoners = [new PrisonerDto(12, "horse", "Horse", 2)] })
                .SetItem(
                    2,
                    ransoms.Seats[2] with
                    {
                        Prisoners =
                        [
                            new PrisonerDto(13, "boat", "Boat", 0),
                            new PrisonerDto(14, "pawn", "Pawn", 0) { MayGoBack = false },
                        ],
                    }
                ),
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find(".ransom-to button.prisoner").Click();

        // Assert
        Assert.Equal(
            ("prisoner|prisoner waiting", 1),
            (
                string.Join("|", table.FindAll(".ransom-ask button.prisoner").Select(button => button.ClassName)),
                table.FindAll("p.under-investigation").Count
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

    [Fact]
    public void Offer_WhenTheSameOfferIsMadeAgainThisTurn_SaysItWasDeclinedWithoutSendingIt()
    {
        // Arrange - the server would refuse anything more it is sent
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();
        table.Find("button.offer").Click();
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Illegal, "Sent again.");

        // Act
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal("Random (Yellow) declined that offer already!", table.Find("p.problem").TextContent);
    }

    [Fact]
    public void Offer_WhenTheServerCannotBeReached_MayBeMadeAgain()
    {
        // Arrange
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();
        this._page.Server.Failure = new HttpRequestException("unreachable");
        table.Find("button.offer").Click();

        // Act
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal(
            (0, ("offer", "1", "9", string.Empty)),
            (table.FindAll("p.problem").Count, Offered(this._page.Server))
        );
    }

    [Fact]
    public void Poll_WhenTheOfferIsDeclined_SaysSoAsTheDecisionComesBack()
    {
        // Arrange - Black offered; Yellow answers
        using PageContext page = new(TimeSpan.FromMilliseconds(200));
        page.Server.View = Ransoms() with { Version = 2, DecidingSeat = 1, MayOffer = false };
        IRenderedComponent<Table> table = page.Render<Table>(parameters => parameters.Add(x => x.Seating, SeatZero));

        // Act
        page.Server.View = Ransoms() with
        {
            Version = 3,
            Log = [new EventDto(4, EventDto.Declined, 1, "Yellow declined Black's offer.", null, null)],
        };

        // Assert
        table.WaitForAssertion(() =>
            Assert.Contains("declined", table.Find("p.notice").TextContent, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Offer_WhenANewTurnHasBegun_MayBeMadeAgain()
    {
        // Arrange - an offer made, then a turn passes
        this._page.Server.View = Ransoms();
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();
        table.Find("button.offer").Click();
        this._page.Server.View = Ransoms() with { Version = 9, TurnNumber = Ransoms().TurnNumber + 4 };
        table.FindAll("button.action").First(button => !button.ClassList.Contains("offer")).Click();
        table.FindAll(".ransom-to")[0].QuerySelector("button.prisoner")!.Click();

        // Act
        table.Find("button.offer").Click();

        // Assert
        Assert.Equal(
            (0, ("offer", "1", "9", string.Empty)),
            (table.FindAll("p.problem").Count, Offered(this._page.Server))
        );
    }

    [Theory]
    [InlineData(false, "Black offers Random (Yellow) Yellow's Pawn back, as a gift.")]
    [InlineData(true, "Black offers Random (Yellow) Yellow's Pawn back, for Black's Boat.")]
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

    [Theory]
    [InlineData(true, true, 1)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    public void EndGame_WhenTheSeatMayProposeEndingTheGame_IsOffered(bool isSeated, bool mayPropose, int expected)
    {
        // Arrange
        this._page.Server.View = Proposable(mayPropose);

        // Act
        IRenderedComponent<Table> table = this.Render(isSeated ? SeatZero : Seating.Watching(FakeGameServer.GameId));

        // Assert
        Assert.Equal(expected, table.FindAll("button.end-game").Count);
    }

    [Theory]
    [InlineData("wait", "false")]
    [InlineData("resign", "true")]
    public void EndGame_WhenChosenHowToWait_ProposesEndingTheGameSo(string choice, string resigns)
    {
        // Arrange
        this._page.Server.View = Proposable(mayPropose: true);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.end-game").Click();

        // Act
        table.Find($".dialog button.{choice}").Click();

        // Assert
        ActionDto? submitted = this._page.Server.Submitted?.Action;
        Assert.Equal(
            ("propose-end", "0", resigns, 0),
            (
                submitted?.Kind,
                submitted?.Arguments[ActionArguments.Actor],
                submitted?.Arguments[EndProposalDto.ResignsArgument],
                table.FindAll(".dialog").Count
            )
        );
    }

    [Fact]
    public void Rejoin_WhenTheSeatGaveUpItsTurns_IsOfferedInsteadOfEndingTheGameAndTakesThemBack()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with { HasGivenUp = true, MayRejoin = true }
            ),
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);
        int ending = table.FindAll("button.end-game").Count;

        // Act
        table.Find("button.rejoin").Click();

        // Assert
        Assert.Equal(
            (0, "rejoin", "0"),
            (
                ending,
                this._page.Server.Submitted?.Action.Kind,
                this._page.Server.Submitted?.Action.Arguments[ActionArguments.Actor]
            )
        );
    }

    [Fact]
    public void Render_WhenTheVoteOnWaitingForAnotherIsOpen_LetsThePlayerStopWaitingAndSaysHowOthersStand()
    {
        // Arrange - Black is away; Yellow, the player, may vote
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = new AwayDto(0, true, [1], []) };
        IRenderedComponent<Table> table = this.Render(SeatOne);
        (string, string, string) shown = (
            table.Find(".away-vote p").TextContent,
            table.Find("button.keep-waiting").GetAttribute("aria-pressed")!,
            table.Find(".away-vote .hint").TextContent
        );

        // Act
        table.Find("button.stop-waiting").Click();

        // Assert
        Assert.Equal(
            (
                "Black is taking a long time. Keep waiting for them?",
                "true",
                "Stop waiting, and their turns pass, unplayed, until they come back; it takes all of you "
                    + "(0 of 1 so far). A bot playing for them instead takes their seat for the rest of the game."
            ),
            shown
        );
        Assert.Equal(
            ("vote-away", "0", "true"),
            (
                this._page.Server.Submitted?.Action.Kind,
                this._page.Server.Submitted?.Action.Arguments[AwayDto.AbsentArgument],
                this._page.Server.Submitted?.Action.Arguments[AwayDto.StopsArgument]
            )
        );
    }

    [Fact]
    public void Render_WhenAnotherThanTheSeatDecidingIsAway_AsksWhetherToWaitForThemOnTheirTurns()
    {
        // Arrange - Black missed its turns; Yellow, the player, decides, and may vote
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            DecidingSeat = 1,
            Away = new AwayDto(0, true, [1], []),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal("Black is away. Keep waiting for them on their turns?", table.Find(".away-vote p").TextContent);
    }

    [Theory]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 0)]
    public void Render_WhenThePlayerMayVoteOnWaiting_ShowsTheVoteOnceOpenOrWhileTheyWouldStop(
        bool isOpen,
        bool isStopping,
        int expected
    )
    {
        // Arrange
        AwayDto away = new(0, isOpen, [1], isStopping ? [1] : []);
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = away };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(expected, table.FindAll(".away-vote").Count);
    }

    [Theory]
    [InlineData(true, 1, 0, 1, "")]
    [InlineData(true, 0, 0, 1, "")]
    [InlineData(true, 0, 270.2, 0, "If they do not come back, you may have a bot play for them in 4 min 31 s.")]
    [InlineData(true, 0, 30, 0, "If they do not come back, you may have a bot play for them in 30 s.")]
    [InlineData(false, 1, 0, 0, "")]
    public void Render_WhenAPlayerIsAway_OffersABotForThemToTheFirstAtOnceAndToOthersAfterAWait(
        bool isOpen,
        int firstArrival,
        double secondsToWait,
        int buttons,
        string hint
    )
    {
        // Arrange
        AwayDto away = new(0, isOpen, [1], [1], firstArrival, secondsToWait);
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = away };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(
            (buttons, hint),
            (
                table.FindAll("button.replace").Count,
                string.Concat(table.FindAll("p.replace-wait").Select(wait => wait.TextContent))
            )
        );
    }

    [Fact]
    public void Replace_WhenClicked_HasABotPlayForThePlayerAway()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = new AwayDto(0, true, [1], [], 1) };
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Act
        table.Find("button.replace").Click();

        // Assert
        Assert.Contains($"replace {FakeGameServer.GameId} {FakeGameServer.Token}", this._page.Server.Calls);
    }

    [Fact]
    public void Render_WhenThePlayerIsNotAmongTheVoters_ShowsNoVote()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = new AwayDto(0, true, [3], []) };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Empty(table.FindAll(".away-vote"));
    }

    [Fact]
    public void Render_WhenOnlyWatchingAVoteOnWaiting_ShowsNeitherTheVoteNorANotice()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = new AwayDto(0, true, [1], [1]) };

        // Act
        IRenderedComponent<Table> table = this.Render(Seating.Watching(FakeGameServer.GameId));

        // Assert
        Assert.Equal((0, 0), (table.FindAll(".away-vote").Count, table.FindAll(".away-notice").Count));
    }

    [Theory]
    [InlineData(2, true, false, "The others are playing on without you: you missed 2 turns in a row.", 1)]
    [InlineData(1, false, true, "The others are playing on without you: you missed 1 turn in a row.", 0)]
    [InlineData(1, false, false, "You are back: the others will wait for you again, unless you miss another turn.", 0)]
    [InlineData(0, false, false, "Random (Yellow) would stop waiting for you.", 0)]
    [InlineData(0, true, true, "Random (Yellow) would stop waiting for you.", 0)]
    public void Render_WhenThePlayerIsAway_SaysWhatTheOthersDidAndOffersToRejoin(
        int missed,
        bool mayRejoin,
        bool hasGivenUp,
        string expected,
        int buttons
    )
    {
        // Arrange - Yellow would stop waiting for Black, the player
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Away = new AwayDto(0, true, [1], [1]),
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with
                {
                    TurnsMissed = missed,
                    MayRejoin = mayRejoin,
                    HasGivenUp = hasGivenUp,
                }
            ),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (expected, buttons),
            (table.Find(".away-notice p").TextContent, table.FindAll(".away-notice button.rejoin.flashing").Count)
        );
    }

    [Fact]
    public void Rejoin_WhenTheFlashingButtonIsClicked_SaysThePlayerIsBack()
    {
        // Arrange
        this._page.Server.View = FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                0,
                FakeGameServer.SeatZeroToMove.Seats[0] with { TurnsMissed = 1, MayRejoin = true }
            ),
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Act
        table.Find("button.rejoin.flashing").Click();

        // Assert
        Assert.Equal(
            ("rejoin", 0),
            (this._page.Server.Submitted?.Action.Kind, table.FindAll(".end-area button.rejoin").Count)
        );
    }

    [Fact]
    public void Render_WhenTheSeatHasAChance_OffersItFlashingEvenOutOfTurn()
    {
        // Arrange
        this._page.Server.View = FaceOffChance();

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal("Face-off!", table.Find(".chances button.chance.flashing").TextContent.Trim());
    }

    [Fact]
    public void Chance_WhenClicked_TakesIt()
    {
        // Arrange
        this._page.Server.View = FaceOffChance();
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Act
        table.Find("button.chance").Click();

        // Assert
        Assert.Equal(
            ("face-off", 0),
            (this._page.Server.Submitted?.Action.Kind, table.FindAll("p.problem").Count)
        );
    }

    [Fact]
    public void Chance_WhenItHasGoneMeanwhile_SaysItIsTooLate()
    {
        // Arrange
        this._page.Server.View = FaceOffChance();
        IRenderedComponent<Table> table = this.Render(SeatOne);
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Illegal, "Not now.");

        // Act
        table.Find("button.chance").Click();

        // Assert
        Assert.Equal(
            "Too late, sorry: the two leaders no longer face each other.",
            table.Find("p.problem").TextContent
        );
    }

    [Fact]
    public void Chance_WhenThePieceToTakeInPassingHasGoneMeanwhile_SaysItIsTooLate()
    {
        // Arrange
        this._page.Server.View = FaceOffChance("move");
        IRenderedComponent<Table> table = this.Render(SeatOne);
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Illegal, "Not now.");

        // Act
        table.Find("button.chance").Click();

        // Assert
        Assert.Equal(
            "Too late, sorry: that piece can no longer be taken in passing.",
            table.Find("p.problem").TextContent
        );
    }

    /// <summary>Seat 0 to move, while seat 1 has a chance: a face-off, unless of another <paramref name="kind"/>.
    /// </summary>
    private static GameViewDto FaceOffChance(string kind = "face-off") =>
        FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                1,
                FakeGameServer.SeatZeroToMove.Seats[1] with
                {
                    Chances =
                    [
                        new ActionDto(
                            kind,
                            ImmutableSortedDictionary<string, string>.Empty.Add("caller", "3").Add("faced", "43")
                        )
                        {
                            Label = "Face-off!",
                        },
                    ],
                }
            ),
        };

    [Fact]
    public void Render_WhenNoOneWouldStopWaitingForTheAbsentPlayer_SaysNothingOfIt()
    {
        // Arrange - another seat is the one waited for
        this._page.Server.View = FakeGameServer.SeatZeroToMove with { Away = new AwayDto(1, true, [0], [0]) };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Empty(table.FindAll(".away-notice"));
    }

    [Fact]
    public void EndGame_WhenCancelled_KeepsPlaying()
    {
        // Arrange
        this._page.Server.View = Proposable(mayPropose: true);
        IRenderedComponent<Table> table = this.Render(SeatZero);
        table.Find("button.end-game").Click();
        int asked = table.FindAll(".dialog").Count;

        // Act
        table.Find(".dialog button.cancel").Click();

        // Assert
        Assert.Equal((1, 0, null), (asked, table.FindAll(".dialog").Count, this._page.Server.Submitted));
    }

    [Fact]
    public void Render_WhenAskedWhetherToEndTheGame_AsksWithTheScoresAndTheTimeLeft()
    {
        // Arrange - Yellow, with 5 points, proposed, giving up its turns
        this._page.Server.View = Proposed(deciding: 0);

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            (
                "Random (Yellow) proposes ending the game",
                "They have given up their remaining turns.",
                "60 s left; no answer keeps the game going.",
                "Random (Yellow) proposes ending the game: do you agree?"
            ),
            (
                table.Find(".dialog h2").TextContent,
                table.FindAll(".dialog p")[0].TextContent,
                table.Find(".dialog .countdown").TextContent,
                table.Find("p.status").TextContent
            )
        );
        Assert.Contains("Black 0, Yellow 5", table.FindAll(".dialog p")[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Answer_WhenClicked_SubmitsIt()
    {
        // Arrange - Yellow waits for the answers, which no clock hurries
        this._page.Server.View = Proposed(deciding: 0) with
        {
            Proposal = new EndProposalDto(1, Resigns: false),
            SecondsLeft = null,
        };
        IRenderedComponent<Table> table = this.Render(SeatZero);
        int said = table.FindAll(".dialog p").Count;

        // Act
        table.Find(".dialog button.answer").Click();

        // Assert
        Assert.Equal(
            ("answer-end", "true", 1),
            (this._page.Server.Submitted?.Action.Kind, this._page.Server.Submitted?.Action.Arguments["agrees"], said)
        );
    }

    [Fact]
    public void Render_WhenAnotherSeatIsAskedWhetherToEndTheGame_SaysWhoWithoutAsking()
    {
        // Arrange
        this._page.Server.View = Proposed(deciding: 0);

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(
            ("Random (Yellow) proposes ending the game: waiting for Black.", 0, "resting on the lead"),
            (
                table.Find("p.status").TextContent,
                table.FindAll(".dialog").Count,
                table.Find(".seat .gave-up").TextContent
            )
        );
    }

    [Theory]
    [InlineData(0, "gave up")]
    [InlineData(5, "resting on the lead")]
    public void Render_WhenASeatHasGivenUpItsTurns_SaysWhetherItGaveUpOrRestsOnItsLead(int score, string expected)
    {
        // Arrange - Yellow gave up its turns, with this score; Black has none
        this._page.Server.View = Proposed(deciding: 0) with
        {
            Seats = Proposed(deciding: 0).Seats.SetItem(1, Proposed(deciding: 0).Seats[1] with { Score = score }),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(expected, table.Find(".seat .gave-up").TextContent);
    }

    [Fact]
    public void Render_WhenASeatHasRetired_SaysSoInsteadOfThatItGaveUp()
    {
        // Arrange - Yellow retired, having given up its turns
        this._page.Server.View = Proposed(deciding: 0) with
        {
            Seats = Proposed(deciding: 0).Seats.SetItem(1, Proposed(deciding: 0).Seats[1] with { IsRetired = true }),
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatOne);

        // Assert
        Assert.Equal(("retired", 0), (table.Find(".seat .retired").TextContent, table.FindAll(".seat .gave-up").Count));
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
                "Black's Boat held by you|Green's Elephant held by Random (Yellow)|Yellow's Horse held by you",
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
    public void Render_WhenPiecesOfTwoEnemyArmiesMayBePromotedInto_ShowsEachArmyApartTheMostValuableFirst()
    {
        // Arrange - Black holds Red's Raja 12 and Horse 13, and Yellow's Horse 9, listed by worth
        GameViewDto promotions = Promotions();
        this._page.Server.View = promotions with
        {
            PromotionDue = 2,
            Seats =
            [
                promotions.Seats[0] with
                {
                    Prisoners =
                    [
                        new PrisonerDto(9, "horse", "Horse", 1),
                        new PrisonerDto(12, "raja", "Raja", 3),
                        new PrisonerDto(13, "horse", "Horse", 3),
                    ],
                },
                promotions.Seats[1] with { Prisoners = [] },
                promotions.Seats[2],
                new SeatDto(3, "Red", promotions.Seats[1].Team, 0, "open", null, []),
            ],
            LegalActions = [FakeGameServer.Pass, Promotion(12), Promotion(9), Promotion(13)],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ["Yellow's Horse", "Red's Raja|Red's Horse"],
            table.FindAll(".promotion-choices")
                .Select(army => string.Join('|', army.QuerySelectorAll(".name").Select(name => name.TextContent)))
        );
    }

    [Fact]
    public void Render_WhenAPieceMayBePromotedByItsOwnPromotion_ShowsThoseChoicesFirstEachByItsKindsName()
    {
        // Arrange - the Pawn may become a Queen, which the game tells of, or a Zebra, which it does not
        GameViewDto promotions = Promotions();
        this._page.Server.View = promotions with
        {
            PromotionDue = 2,
            Kinds = [new PieceKindDto("queen", "Queen (Western)", 9)],
            LegalActions = [.. promotions.LegalActions, OwnPromotion("queen"), OwnPromotion("zebra")],
        };

        // Act
        IRenderedComponent<Table> table = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Queen (Western) its own promotion|zebra its own promotion", 4),
            (
                string.Join('|', table.FindAll(".promotion-choices.own .choice").Select(Choice)),
                table.FindAll(".promotion-choices").Count
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
    private static EventDto RajaTaken(int owner) =>
        new(10 + owner, EventDto.Captured, 1 - owner, "A Raja was taken.", null, null, owner, IsLeader: true);

    private static EventDto Escaped(int owner) =>
        new(7 + owner, EventDto.Escaped, 1, "A piece escaped.", null, 9, owner);

    /// <summary>A Raja of <paramref name="owner"/>'s back on the board, escaped or ransomed home.</summary>
    private static EventDto RajaBack(string kind, int owner) =>
        new(20 + owner, kind, owner, "A Raja is back.", null, 9, owner, IsLeader: true);

    /// <summary>A command, or an empire, of <paramref name="seat"/>'s begun or over.</summary>
    private static EventDto Power(string kind, int seat, bool isOver) =>
        new(30 + seat, kind, seat, "Power shifted.", null, null, IsOver: isOver);

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

    private static ActionDto OwnPromotion(string becomes) =>
        new(
            "promote-own",
            ImmutableSortedDictionary<string, string>.Empty
                .Add(ActionArguments.Actor, "0")
                .Add(ActionArguments.Cell, "2")
                .Add(ActionArguments.Becomes, becomes)
        );

    /// <summary>The game waiting for someone to take Yellow's seat, open, before it begins.</summary>
    private static GameViewDto WaitingForYellow() =>
        FakeGameServer.SeatZeroToMove with
        {
            Seats = FakeGameServer.SeatZeroToMove.Seats.SetItem(
                1,
                FakeGameServer.SeatZeroToMove.Seats[1] with { Controller = "open", Bot = null, BotId = null }
            ),
            LegalActions = [],
        };

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
