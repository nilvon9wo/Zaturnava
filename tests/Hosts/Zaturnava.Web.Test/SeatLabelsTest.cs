using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class SeatLabelsTest
{
    [Theory]
    [InlineData("bot", "Argus", null, "Argus")]
    [InlineData("human", null, "Ada", "Ada")]
    [InlineData("open", null, null, "open")]
    public void Person_WhenAsked_NamesTheBotOrThePersonOrSaysTheSeatIsOpen(
        string controller,
        string? bot,
        string? player,
        string expected
    )
    {
        // Arrange
        SeatDto seat = Seat(controller, bot, player);

        // Act
        string person = SeatLabels.Person(seat);

        // Assert
        Assert.Equal(expected, person);
    }

    [Theory]
    [InlineData("bot", "Argus", null, "Argus (Black)")]
    [InlineData("human", null, "Ada (guest)", "Ada (guest) (Black)")]
    [InlineData("open", null, null, "Black")]
    public void Of_WhenAsked_NamesWhoPlaysTheSeatBeforeTheSeat(
        string controller,
        string? bot,
        string? player,
        string expected
    )
    {
        // Arrange
        SeatDto seat = Seat(controller, bot, player);

        // Act
        string label = SeatLabels.Of(seat);

        // Assert
        Assert.Equal(expected, label);
    }

    [Theory]
    [InlineData("Ada", "Ada · you")]
    [InlineData("Ada (guest)", "Ada (guest) · you")]
    [InlineData(GameSummaryDto.Guest, "You")]
    [InlineData(null, "You")]
    public void Mine_WhenAsked_NamesThePlayerAsYou(string? player, string expected)
    {
        // Arrange
        SeatDto seat = Seat("human", null, player);

        // Act
        string label = SeatLabels.Mine(seat);

        // Assert
        Assert.Equal(expected, label);
    }

    private static SeatDto Seat(string controller, string? bot, string? player) =>
        new(0, "Black", 0, 0, controller, bot, [], Player: player);
}
