using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class PositionEncoderTest
{
    // On the line: 4 seats, Pawns and Rajas (sorted), each counted and how far come; and 9 of what Sage counts.
    private const double Piece = 1 / 8d;
    private const int PrisonersAt = 4 * 2 * 2;
    private const int ScoresAt = PrisonersAt + (4 * 4 * 2);
    private const int DecidingAt = ScoresAt + 4;
    private const int DiceAt = DecidingAt + 4;
    private const int AwaitingAt = DiceAt + PositionEncoder.DieFaces;
    private const int ResignedAt = AwaitingAt + 1;
    private const int RetiredAt = ResignedAt + 4;
    private const int FeaturesAt = RetiredAt + 4;

    private static readonly LineRules Rules = new([]);
    private static readonly GameState Start = Rules.CreateInitialState();

    [Fact]
    public void Size_WhenAsked_CountsEveryNumberAPositionIs()
    {
        // Arrange
        PositionEncoder encoder = new(Rules);

        // Act
        int size = encoder.Size;

        // Assert
        Assert.Equal(FeaturesAt + 9, size);
    }

    [Fact]
    public void Size_WhenTheRulesetHasNoMeasuresOfItsOwn_CountsOnlyWhatSageCountsOfAny()
    {
        // Arrange - 4 seats, and Rajas only
        PositionEncoder encoder = new(new ScriptedRules([]));

        // Act
        int size = encoder.Size;

        // Assert
        Assert.Equal((4 * 2) + (4 * 4) + 4 + 4 + PositionEncoder.DieFaces + 1 + 4 + 4 + 8, size);
    }

    [Fact]
    public void Weighing_WhenGivenSagesWeights_WeighsWhatSageCountsAsTheyDoForTheScaleTheyAreShownIn()
    {
        // Arrange - the line's one measure weighing 4, and one it does not have, 5
        SageWeights weights = new(1, 2, 3, 4, 5, 6, 7)
        {
            Mobility = 8,
            Measures = ImmutableSortedDictionary<string, double>.Empty.Add("pawns", 4).Add("other", 5),
        };

        // Act
        ImmutableList<(int Index, double Weight)> weighing = new PositionEncoder(Rules).Weighing(weights);

        // Assert - each shown as a tenth of itself
        Assert.Equal(
            Enumerable.Range(1, 8).Append(4).Select((weight, at) => (Index: FeaturesAt + at, Weight: weight * 10d)),
            weighing
        );
    }

    [Fact]
    public void Weighing_WhenTheRulesetHasNoMeasuresOfItsOwn_WeighsOnlyWhatSageCountsOfAny()
    {
        // Arrange
        PositionEncoder encoder = new(new ScriptedRules([]));

        // Act
        ImmutableList<(int Index, double Weight)> weighing = encoder.Weighing(SageWeights.Default);

        // Assert
        Assert.Equal((8, encoder.Size - 1), (weighing.Count, weighing[^1].Index));
    }

    [Fact]
    public void Encode_WhenTheGameStarts_CountsEachArmysPiecesAndHowFarTheyHaveComeAndWhoDecides()
    {
        // Arrange - seat 0's Raja and Pawn at cells 0 and 1, seat 1's at 7 and 6; what Sage counts is even
        PositionEncoder encoder = new(Rules);

        // Act
        ImmutableList<(int Index, double Value)> encoded = encoder.Encode(Start, LineRules.Seat);

        // Assert - its Pawns, a row from its edge, and its Raja, on it; the next seat's, alike; seat 0 deciding
        Assert.Equal(
            [(0, Piece), (1, Piece), (2, Piece), (4, Piece), (5, Piece), (6, Piece), (DecidingAt, 1d)],
            encoded
        );
    }

    [Fact]
    public void Encode_WhenSeenFromTheOtherEnd_CountsItsOwnArmyFirst()
    {
        // Arrange - seat 1 faces down the line, from cell 7
        PositionEncoder encoder = new(Rules);

        // Act
        ImmutableList<(int Index, double Value)> encoded = encoder.Encode(Start, LineRules.Enemy);

        // Assert - its own army as seat 0 sees its own; seat 0, the last seat on from it, and deciding
        Assert.Equal(
            [(0, Piece), (1, Piece), (2, Piece), (12, Piece), (13, Piece), (14, Piece), (DecidingAt + 3, 1d)],
            encoded
        );
    }

    [Fact]
    public void Encode_WhenTheGameHasMoreToIt_ShowsPrisonersScoresDiceAndWhoIsOut()
    {
        // Arrange - a die of 9 is more than is told apart, so would show where seat 1's resigning does
        GameState state = Start with
        {
            DecidingSeat = null,
            Prisoners = [new Prisoner(new Piece(new PieceId(9), LineRules.Pawn, LineRules.Enemy), LineRules.Seat)],
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(LineRules.Seat, 25).Add(LineRules.Enemy, 0),
            Dice = [3, 9],
            AwaitedDice = new DiceRequest(1, 6),
            Resigned = [LineRules.Enemy],
            Retired = [new SeatId(3)],
        };
        PositionEncoder encoder = new(Rules);

        // Act
        ImmutableList<(int Index, double Value)> encoded = encoder.Encode(state, LineRules.Seat);

        // Assert
        Assert.Equal(
            [
                (0, Piece), (1, Piece), (2, Piece), (4, Piece), (5, Piece), (6, Piece), (PrisonersAt + 2, 1d),
                (ScoresAt, 0.5),
                (DiceAt + 2, 1d), (AwaitingAt, 1d), (ResignedAt + 1, 1d), (RetiredAt + 3, 1d),
            ],
            encoded.Where(entry => entry.Index < FeaturesAt)
        );
    }

    [Fact]
    public void Encode_WhenAPieceIsOfAKindTheGameDoesNotStartWith_LeavesItOutButForWhatSageCounts()
    {
        // Arrange - a Horse of seat 0's ally, on the board and held
        Piece horse = new(new PieceId(8), LineRules.Horse, new SeatId(2));
        GameState state = Start with
        {
            Position = Start.Position.Place(new CellId(4), horse),
            Prisoners = [new Prisoner(horse with { Id = new PieceId(9) }, LineRules.Enemy)],
        };
        PositionEncoder encoder = new(Rules);

        // Act
        ImmutableList<(int Index, double Value)> encoded = encoder.Encode(state, LineRules.Seat);

        // Assert
        Assert.Equal(
            encoder.Encode(Start, LineRules.Seat).Where(entry => entry.Index < FeaturesAt),
            encoded.Where(entry => entry.Index < FeaturesAt)
        );
        Assert.Contains(encoded, entry => entry.Index >= FeaturesAt);
    }
}
