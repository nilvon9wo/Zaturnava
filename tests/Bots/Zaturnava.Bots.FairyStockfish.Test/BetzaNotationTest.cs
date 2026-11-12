using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

public sealed class BetzaNotationTest
{
    private static readonly SquareGridTopology Grid = new(8, 8);
    private static readonly Direction North = new(0, 1);
    private static readonly Direction East = new(1, 0);

    [Theory]
    [InlineData("step any way", false, false, "WF")]
    [InlineData("bishop", false, false, "B")]
    [InlineData("rook of 3", false, false, "R3")]
    [InlineData("knight", false, false, "N")]
    [InlineData("alfil", false, false, "A")]
    [InlineData("nightrider", false, false, "NN")]
    [InlineData("pawn step", false, false, "mfW")]
    [InlineData("pawn step", true, false, "mrW")]
    [InlineData("pawn step", true, true, "mlW")]
    [InlineData("pawn step", false, true, "mbW")]
    [InlineData("pawn capture", false, false, "cflFcfrF")]
    [InlineData("pawn capture", false, true, "cbrFcblF")]
    [InlineData("rook", false, false, "R")]
    [InlineData("sideways rook", false, false, "lRrR")]
    [InlineData("lame knight", false, false, "nN")]
    [InlineData("cannon", false, false, "pR")]
    [InlineData("grasshopper", false, false, "gR")]
    public void Of_WhenTheRuleCanBeWritten_WritesItFacingAsItsSeatTurnedRoundForBlack(
        string rule,
        bool isFacingEast,
        bool isBlack,
        string betza
    )
    {
        // Arrange
        MovementRule movement = RuleOf(rule);

        // Act
        string? written = BetzaNotation.Of(movement, Grid, isFacingEast ? East : North, isBlack);

        // Assert
        Assert.Equal(betza, written);
    }

    [Theory]
    [InlineData("sliding from 2")]
    [InlineData("shunning")]
    [InlineData("hopping 2 beyond")]
    [InlineData("leaping 4 and 1")]
    public void Of_WhenTheRuleCannotBeWritten_WritesNothing(string rule)
    {
        // Arrange
        MovementRule movement = RuleOf(rule);

        // Act
        string? written = BetzaNotation.Of(movement, Grid, North, isBlack: false);

        // Assert
        Assert.Null(written);
    }

    [Fact]
    public void Of_WhenAnUnevenLeapGoesOnlySomeWays_WritesNothing()
    {
        // Arrange - two of the Knight's eight leaps
        ShapedTopology board = new(directions: [new Direction(1, 2), new Direction(-1, 2)]);

        // Act
        string? written = BetzaNotation.Of(MovementRule.Leap(DirectionSets.Knight, 1), board, North, false);

        // Assert
        Assert.Null(written);
    }

    [Fact]
    public void Of_WhenEveryRuleOfTheKindCanBeWritten_WritesThemOneAfterAnother()
    {
        // Arrange
        PieceKind pawn = new(new PieceKindId("pawn"), [RuleOf("pawn step"), RuleOf("pawn capture")]);
        PieceKind odd = new(new PieceKindId("odd"), [RuleOf("pawn step"), RuleOf("shunning")]);

        // Act
        (string? Pawn, string? Odd) written =
            (BetzaNotation.Of(pawn, Grid, North, false), BetzaNotation.Of(odd, Grid, North, false));

        // Assert
        Assert.Equal(("mfWcflFcfrF", (string?)null), written);
    }

    private static MovementRule RuleOf(string rule) =>
        rule switch
        {
            "step any way" => MovementRule.Step(DirectionSets.Any),
            "bishop" => MovementRule.Slide(DirectionSets.Diagonal),
            "rook" => MovementRule.Slide(DirectionSets.Orthogonal),
            "rook of 3" => new MovementRule(DirectionSets.Orthogonal, 1, 3, false, true, true),
            "knight" => MovementRule.Leap(DirectionSets.Knight, 1),
            "alfil" => MovementRule.Leap(DirectionSets.Diagonal, 2),
            "nightrider" => MovementRule.Slide(DirectionSets.Knight),
            "pawn step" => MovementRule.Step(DirectionSets.Forward).MoveOnly(),
            "pawn capture" => MovementRule.Step(DirectionSets.ForwardDiagonals).CaptureOnly(),
            "sideways rook" => MovementRule.Slide(DirectionSets.Sideways),
            "lame knight" => MovementRule.Leap(DirectionSets.Knight, 1).Lame(),
            "cannon" => MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue),
            "grasshopper" => MovementRule.Hop(DirectionSets.Orthogonal, 1),
            "sliding from 2" => new MovementRule(DirectionSets.Orthogonal, 2, int.MaxValue, false, true, true),
            "shunning" => MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue)
                .Shunning([new PieceKindId("cannon")]),
            "hopping 2 beyond" => MovementRule.Hop(DirectionSets.Orthogonal, 2),
            _ => MovementRule.Leap(DirectionSets.Leaper(4, 1), 1),
        };
}
