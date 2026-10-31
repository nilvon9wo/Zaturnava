using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Pieces;

public sealed class MovementRuleTest
{
    [Fact]
    public void Step_WhenCalled_MovesOneCellAndMayCapture()
    {
        // Arrange - nothing beyond the arguments

        // Act
        MovementRule rule = MovementRule.Step(DirectionSets.Orthogonal);

        // Assert
        Assert.Equal(new MovementRule(DirectionSets.Orthogonal, 1, 1, false, true, true), rule);
    }

    [Fact]
    public void Slide_WhenCalled_HasNoMaximumDistance()
    {
        // Arrange - nothing beyond the arguments

        // Act
        MovementRule rule = MovementRule.Slide(DirectionSets.Orthogonal);

        // Assert
        Assert.Equal(new MovementRule(DirectionSets.Orthogonal, 1, int.MaxValue, false, true, true), rule);
    }

    [Fact]
    public void Leap_WhenCalled_LeapsExactlyThatFar()
    {
        // Arrange - nothing beyond the arguments

        // Act
        MovementRule rule = MovementRule.Leap(DirectionSets.Diagonal, 2);

        // Assert
        Assert.Equal(new MovementRule(DirectionSets.Diagonal, 2, 2, true, true, true), rule);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Leap_WhenTheDistanceIsNotPositive_ThrowsArgumentException(int distance)
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => MovementRule.Leap(DirectionSets.Diagonal, distance)
        );

        // Assert
        Assert.Equal("distance", exception.ParamName);
    }

    [Fact]
    public void MoveOnly_WhenCalled_CannotCapture()
    {
        // Arrange
        MovementRule step = MovementRule.Step(DirectionSets.Forward);

        // Act
        MovementRule rule = step.MoveOnly();

        // Assert
        Assert.Equal(step with { CanCapture = false }, rule);
    }

    [Fact]
    public void CaptureOnly_WhenCalled_CannotMoveToAnEmptyCell()
    {
        // Arrange
        MovementRule step = MovementRule.Step(DirectionSets.ForwardDiagonals);

        // Act
        MovementRule rule = step.CaptureOnly();

        // Assert
        Assert.Equal(step with { CanMove = false }, rule);
    }

    [Fact]
    public void Hop_WhenCalled_SlidesHoppingAndLandsNoFurtherThanAllowed()
    {
        // Arrange - nothing beyond the arguments

        // Act
        MovementRule rule = MovementRule.Hop(DirectionSets.Any, 1);

        // Assert
        Assert.Equal(
            (true, 1, int.MaxValue, true, true),
            (rule.IsHopping, rule.MaximumBeyond, rule.MaximumDistance, rule.CanMove, rule.CanCapture)
        );
    }

    [Theory]
    [InlineData(0)]
    public void Hop_WhenNothingBeyondIsAllowed_ThrowsArgumentException(int beyond)
    {
        // Arrange - nothing beyond the arguments

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => MovementRule.Hop(DirectionSets.Orthogonal, beyond)
        );

        // Assert
        Assert.Equal("beyond", exception.ParamName);
    }

    [Fact]
    public void Lame_WhenCalled_IsBlockedOnTheWay()
    {
        // Arrange
        MovementRule leap = MovementRule.Leap(DirectionSets.Knight, 1);

        // Act
        MovementRule rule = leap.Lame();

        // Assert
        Assert.Equal((false, true), (leap.IsLame, rule.IsLame));
    }

    [Fact]
    public void Shunning_WhenGivenKinds_ShunsThem()
    {
        // Arrange
        PieceKindId cannon = new("cannon");

        // Act
        MovementRule rule = MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).Shunning([cannon]);

        // Assert
        Assert.Equal([cannon], rule.Shunned);
    }
}
