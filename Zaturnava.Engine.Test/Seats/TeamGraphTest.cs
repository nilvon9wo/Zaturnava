using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Seats;

public sealed class TeamGraphTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);
    private static readonly SeatId Third = new(2);

    private readonly TeamGraph _teams = TeamGraph.FromTeams([[First, Third], [Second]]);

    [Fact]
    public void TeamOf_WhenTheSeatIsInTheSecondTeam_ReturnsOne()
    {
        // Arrange - nothing beyond the teams

        // Act
        int team = this._teams.TeamOf(Second);

        // Assert
        Assert.Equal(1, team);
    }

    [Fact]
    public void AreAllied_WhenTheSeatsShareATeam_ReturnsTrue()
    {
        // Arrange - nothing beyond the teams

        // Act
        bool isAllied = this._teams.AreAllied(First, Third);

        // Assert
        Assert.True(isAllied);
    }

    [Fact]
    public void AreAllied_WhenTheSeatsAreOnDifferentTeams_ReturnsFalse()
    {
        // Arrange - nothing beyond the teams

        // Act
        bool isAllied = this._teams.AreAllied(First, Second);

        // Assert
        Assert.False(isAllied);
    }
}
