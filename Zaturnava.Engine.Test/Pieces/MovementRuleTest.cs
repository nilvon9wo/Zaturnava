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
}
