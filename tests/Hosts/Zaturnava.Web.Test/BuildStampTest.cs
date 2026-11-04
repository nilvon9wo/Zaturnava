using System.Reflection;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class BuildStampTest
{
    [Theory]
    [InlineData("0.0.0-dev+1a2b3c4d5e6f", "Build 1a2b3c4")]
    [InlineData("0.0.0-dev+1a2b", "Build 1a2b")]
    [InlineData("0.0.0-dev", "Build 0.0.0-dev")]
    [InlineData(null, "Build unknown")]
    public void Of_WhenTheVersionNamesACommitOrNot_TellsTheCommitShortenedOrTheVersion(string? version, string expected)
    {
        // Arrange - the version as the build stamped it

        // Act
        string stamp = BuildStamp.Of(version is null ? null : new AssemblyInformationalVersionAttribute(version));

        // Assert
        Assert.Equal(expected, stamp);
    }

    [Fact]
    public void Current_WhenAsked_TellsThisBuild()
    {
        // Arrange - this build's own version

        // Act
        string stamp = BuildStamp.Current;

        // Assert
        Assert.StartsWith("Build ", stamp, StringComparison.Ordinal);
    }
}
