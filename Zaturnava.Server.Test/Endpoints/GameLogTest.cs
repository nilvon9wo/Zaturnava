using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class GameLogTest
{
    private static readonly ClassicRules Rules = new();
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly Piece Horse = new(new PieceId(9), ClassicPieces.Horse, Black);
    private static readonly Piece Pawn = new(new PieceId(20), ClassicPieces.Pawn, Yellow);
    private static readonly Piece RedRaja = new(new PieceId(27), ClassicPieces.Raja, Red);

    [Fact]
    public void LatestOf_WhenTheGameOpens_TellsOfTheRollOff()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(
            new RollOffRolled(Black, 5),
            new RollOffRolled(Yellow, 5),
            new RollOffRolled(Green, 5),
            new RollOffRolled(Red, 2),
            new RollOffTied([Black, Yellow, Green]),
            new RollOffWon(Yellow, 6)
        );

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(
            [
                "rolled-off 0: Black rolled 5 to see who goes first.",
                "rolled-off 1: Yellow rolled 5 to see who goes first.",
                "rolled-off 2: Green rolled 5 to see who goes first.",
                "rolled-off 3: Red rolled 2 to see who goes first.",
                "rolled-off 0: Black, Yellow and Green rolled highest together, and roll again.",
                "rolled-off 1: Yellow goes first, with the 6 (Pawn or Raja) they rolled.",
            ],
            log.Select(Told)
        );
    }

    [Fact]
    public void LatestOf_WhenThingsHappened_TellsOfEachInTheRulesetsNames()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(
            new DiceRolled(Black, [2, 5]),
            new PieceMoved(Horse, Cell("b1"), Cell("c3"), Black),
            new PieceCaptured(Pawn, Black, 1),
            new TurnPassed(Black, Yellow),
            new PieceMoved(Pawn, Cell("h2"), Cell("g2"), Yellow),
            new TurnPassed(Yellow, Green)
        );

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(
            [
                "rolled 0: Black rolled 2 (Horse) and 5 (Pawn or Raja).",
                "moved 0: Black moved their Horse from b1 to c3. 1-18",
                "captured 0: Black took Yellow's Pawn (+1).",
                "passed 0: Yellow to play.",
                "moved 1: Yellow moved their Pawn from h2 to g2, without rolling. 15-14",
                "passed 1: Green to play.",
            ],
            log.Select(Told)
        );
    }

    [Fact]
    public void LatestOf_WhenThronesComeIntoPlay_TellsOfTheBonusesAndTheSharedTurn()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(
            new TurnShared(Red, Black),
            new PieceMoved(Pawn, Cell("h2"), Cell("g2"), Black),
            new ThroneEntered(RedRaja, Yellow, 1),
            new PieceCaptured(RedRaja, Black, 5),
            new RajaCapturedOnThrone(RedRaja, Black, 2)
        );

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(
            [
                "shared 0: Black holds Red's throne, so moves for Red now.",
                "moved 0: Black moved Yellow's Pawn from h2 to g2, without rolling. 15-14",
                "bonus 3: Red's Raja entered Yellow's throne (+1).",
                "captured 0: Black took Red's Raja (+5).",
                "bonus 0: Black took that Raja on its own throne (+2 more).",
            ],
            log.Select(Told)
        );
    }

    [Fact]
    public void LatestOf_WhenPrisonersArePromotedOrRansomed_TellsOfIt()
    {
        // Arrange
        Piece blackPawn = new(new PieceId(4), ClassicPieces.Pawn, Black);
        Piece elephant = new(new PieceId(18), ClassicPieces.Elephant, Green);
        RansomOffer gift = new(Black, Yellow, [Pawn.Id], []);
        RansomOffer trade = new(Black, Yellow, [Pawn.Id, new PieceId(21)], [Horse.Id]);
        GameSnapshot snapshot = Snapshot(
            new PiecePromoted(Pawn, Horse, Cell("h8"), Black),
            new PiecePromoted(blackPawn, elephant, Cell("c8"), Black),
            new PieceCaptured(Horse, Black, 0),
            new RansomWindowOpened(Black),
            new RansomOffered(gift),
            new RansomDeclined(gift),
            new RansomOffered(trade),
            new RansomAccepted(trade),
            new PieceReturned(Pawn, Cell("a7"))
        );

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(
            [
                "promoted 0: Black promoted Yellow's Pawn on h8 to their Horse.",
                "promoted 0: Black promoted their Pawn on c8 to Green's Elephant.",
                "captured 0: Black took their Horse (no points).",
                "ransom 0: Black's turn is over, but they may still offer prisoners back.",
                "ransom 0: Black offered Yellow a prisoner back, as a gift.",
                "ransom 1: Yellow declined Black's offer.",
                "ransom 0: Black offered Yellow 2 prisoners back, for a prisoner in return.",
                "ransom 1: Yellow accepted Black's offer.",
                "ransom 1: Yellow's Pawn went home, to a7.",
            ],
            log.Select(Told)
        );
        Assert.Equal((Cell("h8").Value, Cell("a7").Value), (log[0].To, log[^1].To));
    }

    [Theory]
    [InlineData(new[] { 0, 2 }, "ended 0: Black and Green won: only one team has pieces left.")]
    [InlineData(new[] { 1 }, "ended 1: Yellow won: only one team has pieces left.")]
    [InlineData(new int[0], "ended 0: The game is drawn: only one team has pieces left.")]
    public void LatestOf_WhenTheGameEnded_TellsHow(int[] seats, string expected)
    {
        // Arrange
        ImmutableList<SeatId> winners = [.. seats.Select(seat => new SeatId(seat))];
        GameSnapshot snapshot = Snapshot(new GameEnded(new GameOutcome(winners, "only one team has pieces left")));

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(expected, Told(log.Single()));
    }

    [Fact]
    public void LatestOf_WhenMoreHappenedThanALogHolds_TellsOfTheLatest()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot([.. Enumerable.Repeat<GameEvent>(new TurnPassed(Black, Yellow), 50)]);

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal((GameLog.Length, 10, 49), (log.Count, log[0].Number, log[^1].Number));
    }

    [Fact]
    public void LatestOf_WhenSomethingHappenedThatItCannotTell_ThrowsNotSupportedException()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(new Mystery());

        // Act
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => GameLog.LatestOf(snapshot));

        // Assert
        Assert.StartsWith("There is no way yet to tell of ", exception.Message, StringComparison.Ordinal);
    }

    private static string Told(EventDto happened) =>
        happened.From is { } from
            ? $"{happened.Kind} {happened.Seat}: {happened.Text} {from}-{happened.To}"
            : $"{happened.Kind} {happened.Seat}: {happened.Text}";

    private static CellId Cell(string name) => Rules.Topology.Cells.Single(cell => Rules.Topology.NameOf(cell) == name);

    private static GameSnapshot Snapshot(params GameEvent[] events)
    {
        GameRecord record = new(new GameId("g"), Rules.Id, [], [], 0);
        return new GameSnapshot(record, Rules, Rules.CreateInitialState(), [.. events], []);
    }
}

/// <summary>Something no log knows how to tell of.</summary>
file sealed record Mystery : GameEvent;
