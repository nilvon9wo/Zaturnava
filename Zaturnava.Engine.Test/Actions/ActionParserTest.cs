using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class ActionParserTest
{
    [Fact]
    public void Parse_WhenGivenADescribedMove_ReturnsTheSameMove()
    {
        // Arrange
        MoveAction move = new(new SeatId(1), new CellId(8), new CellId(16));

        // Act
        GameAction parsed = ActionParser.Standard.Parse(move.Describe());

        // Assert
        Assert.Equal(move, parsed);
    }

    [Fact]
    public void Parse_WhenGivenADescribedPass_ReturnsTheSamePass()
    {
        // Arrange
        PassAction pass = new(new SeatId(3));

        // Act
        GameAction parsed = ActionParser.Standard.Parse(pass.Describe());

        // Assert
        Assert.Equal(pass, parsed);
    }

    [Fact]
    public void Parse_WhenTheKindIsUnknown_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new("castle", ImmutableSortedDictionary<string, string>.Empty);

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => ActionParser.Standard.Parse(descriptor)
        );

        // Assert
        Assert.Equal("There is no 'castle' action.", exception.Message);
    }
}
