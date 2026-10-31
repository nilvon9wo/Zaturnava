namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class RefusalStatusTest
{
    [Theory]
    [InlineData(Refusal.Invalid, 400)]
    [InlineData(Refusal.NotFound, 404)]
    [InlineData(Refusal.Forbidden, 403)]
    [InlineData(Refusal.Conflict, 409)]
    [InlineData(Refusal.Illegal, 422)]
    [InlineData(Refusal.TooBusy, 429)]
    public void CodeOf_WhenGivenARefusal_ReturnsItsStatus(Refusal refusal, int expected)
    {
        // Arrange - nothing beyond the arguments

        // Act
        int code = RefusalStatus.CodeOf(refusal);

        // Assert
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData(Refusal.Invalid)]
    [InlineData(Refusal.NotFound)]
    [InlineData(Refusal.Forbidden)]
    [InlineData(Refusal.Conflict)]
    [InlineData(Refusal.Illegal)]
    [InlineData(Refusal.TooBusy)]
    public void RefusalOf_WhenGivenARefusalsStatus_ReturnsTheRefusal(Refusal refusal)
    {
        // Arrange
        int code = RefusalStatus.CodeOf(refusal);

        // Act
        Refusal? reported = RefusalStatus.RefusalOf(code);

        // Assert
        Assert.Equal(refusal, reported);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    public void RefusalOf_WhenTheStatusReportsNoRefusal_ReturnsNull(int code)
    {
        // Arrange - nothing beyond the arguments

        // Act
        Refusal? reported = RefusalStatus.RefusalOf(code);

        // Assert
        Assert.Null(reported);
    }
}
