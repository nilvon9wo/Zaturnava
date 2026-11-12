using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Pieces;

public sealed class MovementBearingTest
{
    public static TheoryData<string, string> Pieces { get; } = new()
    {
        { "pawn", MovementBearing.Forward },
        { "rook", MovementBearing.Orthogonal },
        { "bishop", MovementBearing.Diagonal },
        { "queen", MovementBearing.EveryWay },
        { "dragon-king", MovementBearing.EveryWay },
        { "knight", MovementBearing.Other },
        { "zebra", MovementBearing.Other },
        { "silver-general", MovementBearing.Other },
        { "amazon", MovementBearing.EveryWay },
    };

    [Theory]
    [MemberData(nameof(Pieces))]
    public void Of_WhenGivenAPiecesMovement_TellsWhichWayItGoesItsLeapsAside(string piece, string expected)
    {
        // Arrange
        ImmutableList<MovementRule> movement = Movement(piece);

        // Act
        string bearing = MovementBearing.Of(movement);

        // Assert
        Assert.Equal(expected, bearing);
    }

    [Fact]
    public void All_WhenAsked_OffersEveryBearingInOrder()
    {
        // Arrange - nothing

        // Act
        string all = string.Join(",", MovementBearing.All);

        // Assert
        Assert.Equal("Forward,Orthogonal,Diagonal,Every way,Other", all);
    }

    private static ImmutableList<MovementRule> Movement(string piece) =>
        piece switch
        {
            "pawn" =>
            [
                MovementRule.Step(DirectionSets.Forward).MoveOnly(),
                MovementRule.Step(DirectionSets.ForwardDiagonals).CaptureOnly(),
            ],
            "rook" => [MovementRule.Slide(DirectionSets.Orthogonal)],
            "bishop" => [MovementRule.Slide(DirectionSets.Diagonal)],
            "queen" => [MovementRule.Slide(DirectionSets.Any)],
            "dragon-king" => [MovementRule.Slide(DirectionSets.Orthogonal), MovementRule.Step(DirectionSets.Diagonal)],
            "knight" => [MovementRule.Leap(DirectionSets.Knight, 1)],
            "zebra" => [MovementRule.Leap(DirectionSets.Leaper(3, 2), 1)],
            "silver-general" => [MovementRule.Step(DirectionSets.Diagonal), MovementRule.Step(DirectionSets.Forward)],
            _ => [MovementRule.Slide(DirectionSets.Any), MovementRule.Leap(DirectionSets.Knight, 1)],
        };
}
