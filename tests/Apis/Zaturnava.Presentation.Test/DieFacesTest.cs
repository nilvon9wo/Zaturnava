namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class DieFacesTest
{
    [Theory]
    [InlineData(1, "⚀")]
    [InlineData(6, "⚅")]
    [InlineData(0, "0")]
    [InlineData(8, "8")]
    public void FaceOf_WhenGivenAValue_IsItsFaceOrElseItsNumber(int value, string expected)
    {
        // Arrange - nothing beyond the value

        // Act
        string face = DieFaces.FaceOf(value);

        // Assert
        Assert.Equal(expected, face);
    }
}
