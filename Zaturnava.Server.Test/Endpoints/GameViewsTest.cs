using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class GameViewsTest
{
    private static readonly ClassicRules Rules = new();
    private static readonly ImmutableList<BotProfile> Bots = [new("midas", "Midas", "Greedy.", false)];

    [Fact]
    public void ViewOf_WhenTheGameIsOver_ShowsNoDecidingSeatAndHowItEnded()
    {
        // Arrange
        GameOutcome outcome = new([new SeatId(0), new SeatId(2)], "only one team has pieces left");
        GameSnapshot over = Snapshot(Rules.CreateInitialState() with { DecidingSeat = null, Outcome = outcome });

        // Act
        GameViewDto view = GameViews.ViewOf(over, Bots);

        // Assert
        Assert.Equal(
            (null, "0,2", "Black and Green won: only one team has pieces left."),
            (view.DecidingSeat, string.Join(",", view.Outcome!.Winners), view.Outcome.Text)
        );
    }

    [Fact]
    public void ViewOf_WhenTheGameIsADraw_SaysSo()
    {
        // Arrange
        GameOutcome outcome = new([], "only Rajas are left, an honourable peace");
        GameSnapshot over = Snapshot(Rules.CreateInitialState() with { DecidingSeat = null, Outcome = outcome });

        // Act
        GameViewDto view = GameViews.ViewOf(over, Bots);

        // Assert
        Assert.Equal("The game is drawn: only Rajas are left, an honourable peace.", view.Outcome?.Text);
    }

    [Fact]
    public void ViewOf_WhenASeatActsForAnother_ShowsBoth()
    {
        // Arrange
        GameSnapshot shared = Snapshot(
            Rules.CreateInitialState() with { DecidingSeat = new SeatId(3), ActingFor = new SeatId(1) }
        );

        // Act
        GameViewDto view = GameViews.ViewOf(shared, Bots);

        // Assert
        Assert.Equal((3, 1), (view.DecidingSeat, view.ActingFor));
    }

    [Fact]
    public void ViewOf_WhenSeatsHoldPrisoners_ShowsEachWithItsHolder()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), ClassicPieces.Pawn, new SeatId(1));
        GameSnapshot snapshot = Snapshot(
            Rules.CreateInitialState() with { Prisoners = [new Prisoner(pawn, new SeatId(0))] }
        );

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(
            ("pawn Pawn 1", 0),
            (
                view.Seats[0].Prisoners.Select(held => $"{held.Kind} {held.Name} {held.Owner}").Single(),
                view.Seats[1].Prisoners.Count
            )
        );
    }

    [Fact]
    public void ViewOf_WhenARansomWaitsOnItsAnswer_ShowsItsPrisonersAndTheTimeLeft()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), ClassicPieces.Pawn, new SeatId(1));
        Piece horse = new(new PieceId(9), ClassicPieces.Horse, new SeatId(0));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = new SeatId(1),
            Prisoners = [new Prisoner(pawn, new SeatId(0)), new Prisoner(horse, new SeatId(1))],
            Offer = new RansomOffer(new SeatId(0), new SeatId(1), [pawn.Id], [horse.Id]),
        };
        GameSnapshot snapshot = Snapshot(state) with { TimeLeft = TimeSpan.FromSeconds(12.5) };

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(
            ("0>1: 20 Pawn 1 for 9 Horse 0", false, 12.5),
            (
                $"{view.Offer!.From}>{view.Offer.To}: {Listed(view.Offer.Given)} for {Listed(view.Offer.Asked)}",
                view.MayOffer,
                view.SecondsLeft
            )
        );
    }

    [Fact]
    public void ViewOf_WhenTheDecidingSeatHoldsPrisonersOnItsTurn_SaysItMayOffer()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), ClassicPieces.Pawn, new SeatId(1));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Prisoners = [new Prisoner(pawn, new SeatId(0))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal((true, null, null), (view.MayOffer, view.Offer, view.SecondsLeft));
    }

    [Fact]
    public void ViewOf_WhenAPawnMayBePromoted_LabelsEachPromotion()
    {
        // Arrange - Black's Pawn on c8, the far edge; Green's Elephant is held by Red
        Piece pawn = new(new PieceId(4), ClassicPieces.Pawn, new SeatId(0));
        Piece elephant = new(new PieceId(18), ClassicPieces.Elephant, new SeatId(2));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = Rules.CreateInitialState().Position.Place(Rules.Topology.Cells[58], pawn),
            Prisoners = [new Prisoner(elephant, new SeatId(3))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            ["Promote the Pawn on c8 to Green's Elephant"],
            view.LegalActions.Where(action => action.Kind == "promote").Select(action => action.Label)
        );
    }

    [Fact]
    public void ViewOf_WhenAPromotionIsDue_SaysWhereAndLabelsPassingAsLater()
    {
        // Arrange - Black's Pawn has just come to c8
        Piece pawn = new(new PieceId(4), ClassicPieces.Pawn, new SeatId(0));
        Piece elephant = new(new PieceId(18), ClassicPieces.Elephant, new SeatId(2));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            Position = Rules.CreateInitialState().Position.Place(Rules.Topology.Cells[58], pawn),
            Prisoners = [new Prisoner(elephant, new SeatId(3))],
            PromotionDue = Rules.Topology.Cells[58],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal(
            (58, "pass: Promote later"),
            (view.PromotionDue, $"{view.LegalActions[0].Kind}: {view.LegalActions[0].Label}")
        );
    }

    [Fact]
    public void ViewOf_WhenATurnIsOverButItsSeatMayStillOffer_LabelsPassingAsDone()
    {
        // Arrange
        Piece pawn = new(new PieceId(20), ClassicPieces.Pawn, new SeatId(1));
        GameState state = Rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            IsRansomWindow = true,
            Prisoners = [new Prisoner(pawn, new SeatId(0))],
        };

        // Act
        GameViewDto view = GameViews.ViewOf(Snapshot(state), Bots);

        // Assert
        Assert.Equal("pass: Done offering", $"{view.LegalActions.Single().Kind}: {view.LegalActions.Single().Label}");
    }

    [Fact]
    public void ViewOf_WhenSeatsArePlayedByBots_NamesThemAsTheyAreOffered()
    {
        // Arrange
        GameRecord record = new(
            new GameId("bots"),
            Rules.Id,
            [new BotSeat("midas"), new BotSeat("retired"), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        GameSnapshot snapshot = new(record, Rules, Rules.CreateInitialState(), [], []);

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal("Midas|retired||", string.Join("|", view.Seats.Select(seat => seat.Bot)));
    }

    [Fact]
    public void ViewOf_WhenDrawingTheBoard_NamesEachCell()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(Rules.CreateInitialState());

        // Act
        GameViewDto view = GameViews.ViewOf(snapshot, Bots);

        // Assert
        Assert.Equal(("a1", "h8"), (view.Cells[0].Name, view.Cells[63].Name));
    }

    private static string Listed(ImmutableList<PrisonerDto> prisoners) =>
        string.Join(", ", prisoners.Select(prisoner => $"{prisoner.Id} {prisoner.Name} {prisoner.Owner}"));

    private static GameSnapshot Snapshot(GameState state)
    {
        GameRecord record = new(
            new GameId("g"),
            Rules.Id,
            [new OpenSeat(), new OpenSeat(), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        return new GameSnapshot(record, Rules, state, [], Rules.LegalActions(state));
    }
}
