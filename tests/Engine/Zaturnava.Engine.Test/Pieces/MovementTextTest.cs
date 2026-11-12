using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Pieces;

public sealed class MovementTextTest
{
    /// <summary>The rules told, by a name a theory can pass.</summary>
    private static readonly ImmutableDictionary<string, MovementRule> Named =
        ImmutableDictionary<string, MovementRule>.Empty
            .Add("step-orthogonal", MovementRule.Step(DirectionSets.Orthogonal))
            .Add("step-diagonal", MovementRule.Step(DirectionSets.Diagonal))
            .Add("step-any", MovementRule.Step(DirectionSets.Any))
            .Add("step-forward", MovementRule.Step(DirectionSets.Forward))
            .Add("step-forward-diagonals", MovementRule.Step(DirectionSets.ForwardDiagonals))
            .Add("step-sideways", MovementRule.Step(DirectionSets.Sideways))
            .Add("step-hex", MovementRule.Step(new DirectionSet("hex")))
            .Add("slide-orthogonal", MovementRule.Slide(DirectionSets.Orthogonal))
            .Add("slide-knight", MovementRule.Slide(DirectionSets.Knight))
            .Add("one-or-two-forward", new MovementRule(DirectionSets.Forward, 1, 2, false, true, false))
            .Add("leap-knight", MovementRule.Leap(DirectionSets.Knight, 1))
            .Add("leap-knight-lame", MovementRule.Leap(DirectionSets.Knight, 1).Lame())
            .Add("leaper-3-2", MovementRule.Leap(DirectionSets.Leaper(3, 2), 1))
            .Add("leaper-2-2", MovementRule.Leap(DirectionSets.Leaper(2, 2), 1))
            .Add("leaper-3-0", MovementRule.Leap(DirectionSets.Leaper(3, 0), 1))
            .Add("leap-diagonal-2", MovementRule.Leap(DirectionSets.Diagonal, 2))
            .Add("hop-just-beyond", MovementRule.Hop(DirectionSets.Any, 1))
            .Add("hop-any-beyond", MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue));

    [Theory]
    [InlineData("step-orthogonal", "One cell orthogonally")]
    [InlineData("step-diagonal", "One cell diagonally")]
    [InlineData("step-any", "One cell in any direction")]
    [InlineData("step-forward", "One cell straight forward")]
    [InlineData("step-forward-diagonals", "One cell diagonally forward")]
    [InlineData("step-sideways", "One cell sideways")]
    [InlineData("step-hex", "One cell hex")]
    [InlineData("slide-orthogonal", "Any distance orthogonally, up to the first piece in the way")]
    [InlineData("slide-knight", "Any number of Knight's leaps in one line, up to the first piece in the way")]
    [InlineData("one-or-two-forward", "From 1 to 2 cells straight forward, up to the first piece in the way")]
    [InlineData("leap-knight", "Leaps as a Knight: two cells one way and one square to it, over anything between")]
    [InlineData(
        "leap-knight-lame",
        "Leaps as a Knight: two cells one way and one square to it, unless a piece blocks the way"
    )]
    [InlineData("leaper-3-2", "Leaps 3 cells one way and 2 square to it, over anything between")]
    [InlineData("leaper-2-2", "Leaps exactly 2 cells diagonally, over anything between")]
    [InlineData("leaper-3-0", "Leaps exactly 3 cells orthogonally, over anything between")]
    [InlineData("leap-diagonal-2", "Leaps exactly 2 cells diagonally, over anything between")]
    [InlineData(
        "hop-just-beyond",
        "Hops in any direction over the first piece in its line, whether next to it or any number of empty cells "
            + "away, landing just beyond it"
    )]
    [InlineData(
        "hop-any-beyond",
        "Hops orthogonally over the first piece in its line, whether next to it or any number of empty cells away, "
            + "to any cell beyond it up to the next piece"
    )]
    public void Of_WhenAsked_TellsTheRuleForPeople(string name, string expected)
    {
        // Arrange
        MovementRule rule = Named[name];

        // Act
        string text = MovementText.Of(rule, kind => kind.Value);

        // Assert
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Of_WhenTheRuleShunsKinds_NamesThemInOrder()
    {
        // Arrange
        MovementRule rule = MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue)
            .Shunning([new PieceKindId("cannon-x"), new PieceKindId("cannon-j")]);

        // Act
        string text = MovementText.Of(rule, kind => kind.Value.ToUpperInvariant());

        // Assert
        Assert.Equal(
            "Hops orthogonally over the first piece in its line, whether next to it or any number of empty cells "
                + "away, to any cell beyond it up to the next piece, never over or onto a CANNON-J or CANNON-X",
            text
        );
    }
}
