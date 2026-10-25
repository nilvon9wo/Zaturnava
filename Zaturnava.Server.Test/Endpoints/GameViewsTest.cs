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

    private static GameSnapshot Snapshot(GameState state)
    {
        GameRecord record = new(
            new GameId("g"),
            Rules.Id,
            [new OpenSeat(), new OpenSeat(), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        return new GameSnapshot(record, Rules, state, [], []);
    }
}
