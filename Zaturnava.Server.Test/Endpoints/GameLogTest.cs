using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class GameLogTest
{
    private static readonly ClassicRules Rules = new();
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly Piece Horse = new(new PieceId(9), ClassicPieces.Horse, Black);
    private static readonly Piece Pawn = new(new PieceId(20), ClassicPieces.Pawn, Yellow);

    [Fact]
    public void LatestOf_WhenThingsHappened_TellsOfEachInTheRulesetsNames()
    {
        // Arrange
        GameSnapshot snapshot = Snapshot(
            new DiceRolled(Black, [2, 5]),
            new PieceMoved(Horse, new CellId(1), new CellId(18)),
            new PieceCaptured(Pawn, Black, 1),
            new TurnPassed(Black, Yellow)
        );

        // Act
        ImmutableList<EventDto> log = GameLog.LatestOf(snapshot);

        // Assert
        Assert.Equal(
            "0 rolled 0: Black rolled 2 (Horse) and 5 (Pawn or Raja). -|"
                + "1 moved 0: Black's Horse moved. 1-18|"
                + "2 captured 0: Black took Yellow's Pawn (+1). -|"
                + "3 passed 0: Yellow to play. -",
            string.Join(
                "|",
                log.Select(happened =>
                    $"{happened.Number} {happened.Kind} {happened.Seat}: {happened.Text} {happened.From}-{happened.To}"
                )
            )
        );
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

    private static GameSnapshot Snapshot(params GameEvent[] events)
    {
        GameRecord record = new(new GameId("g"), Rules.Id, [], [], 0);
        return new GameSnapshot(record, Rules, Rules.CreateInitialState(), [.. events], []);
    }
}

/// <summary>Something no log knows how to tell of.</summary>
file sealed record Mystery : GameEvent;
