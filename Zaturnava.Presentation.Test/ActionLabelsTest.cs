using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class ActionLabelsTest
{
    [Theory]
    [InlineData("roll", "Roll the dice")]
    [InlineData("pass", "Pass")]
    [InlineData("ransom", "ransom")]
    public void LabelOf_WhenGivenAnAction_SaysWhatItDoes(string kind, string expected)
    {
        // Arrange
        ActionDto action = new(kind, ImmutableSortedDictionary<string, string>.Empty);

        // Act
        string label = ActionLabels.LabelOf(action);

        // Assert
        Assert.Equal(expected, label);
    }
}
