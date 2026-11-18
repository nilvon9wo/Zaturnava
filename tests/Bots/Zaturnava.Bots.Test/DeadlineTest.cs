using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// Each search, out of time before it begins (playtest 2026-11-15, D3), still plays: what it had found so far, which,
/// with nothing looked at past the first step, is the move that looks best at once - moving to 2, worth 5 at once,
/// though the enemy's best reply to it leaves the bot 1, against 4 after moving to 1.
/// </summary>
public sealed class DeadlineTest
{
    private static readonly SageWeights Score = new(1, 0, 0, 0, 0, 0, 0);

    private static readonly TreeRules Rules = new(
        ImmutableDictionary<int, TreeNode>.Empty
            .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
            .Add(1, new TreeNode(TreeRules.Enemy, 0, [3, 4]))
            .Add(2, new TreeNode(TreeRules.Enemy, 5, [5, 6]))
            .Add(3, new TreeNode(TreeRules.Seat, 0, [7, 8]))
            .Add(4, new TreeNode(TreeRules.Seat, 0, [9, 10]))
            .Add(5, new TreeNode(TreeRules.Seat, 0, [11, 12]))
            .Add(6, new TreeNode(TreeRules.Seat, 0, [13, 14]))
            .Add(7, TreeNode.Over(4))
            .Add(8, TreeNode.Over(2))
            .Add(9, TreeNode.Over(3))
            .Add(10, TreeNode.Over(9))
            .Add(11, TreeNode.Over(1))
            .Add(12, TreeNode.Over(0))
            .Add(13, TreeNode.Over(6))
            .Add(14, TreeNode.Over(7))
    );

    [Fact]
    public void IsPast_WhenItsTimeHasOrHasNotRunOut_SaysSo()
    {
        // Arrange
        Deadline now = new(TimeSpan.Zero);
        Deadline later = new(Deadline.Limit);

        // Act
        (bool IsNowPast, bool IsLaterPast) past = (now.IsPast, later.IsPast);

        // Assert
        Assert.Equal((true, false), past);
    }

    [Fact]
    public void Choose_WhenTacticsHasNoTime_PlaysWhatLooksBestAtOnce()
    {
        // Arrange
        Tactics tactics = new(new Appraiser(Score), TacticianBot.Budget, TacticianBot.Width, TimeSpan.Zero);

        // Act
        GameAction action = tactics.Choose(Rules.Request(), new Random(7));

        // Assert
        Assert.Equal(TreeRules.Move(0, 2), action);
    }

    [Fact]
    public void Choose_WhenForesightHasNoTime_PlaysWhatLooksBestAtOnce()
    {
        // Arrange
        Foresight foresight = new(new Appraiser(Score), 3, 3, TimeSpan.Zero);

        // Act
        GameAction action = foresight.Choose(Rules.Request(), new Random(7));

        // Assert
        Assert.Equal(TreeRules.Move(0, 2), action);
    }

    [Fact]
    public void Choose_WhenMonteCarloHasNoTime_StopsAfterItsFirstPass()
    {
        // Arrange - a million passes asked, which would take far longer than the test may
        MonteCarlo search = new(new Appraiser(Score), 1_000_000, 4, 1, TimeSpan.Zero);

        // Act
        GameAction action = search.Choose(Rules.Request(), new Random(7));

        // Assert
        Assert.Contains(action, (IEnumerable<GameAction>)[TreeRules.Move(0, 1), TreeRules.Move(0, 2)]);
    }
}
