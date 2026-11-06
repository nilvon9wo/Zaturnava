namespace Net.NowhereAtAll.Zaturnava.Contracts.Test;

public sealed class GameListQueryTest
{
    [Fact]
    public void QueryString_WhenItAsksNothing_IsEmpty()
    {
        // Arrange
        GameListQuery query = new();

        // Act
        string asked = query.QueryString();

        // Assert
        Assert.Equal(string.Empty, asked);
    }

    [Fact]
    public void QueryString_WhenItAsksEverything_NamesEachFilterAndThePage()
    {
        // Arrange
        GameListQuery query = new("a&b", true, false, true, 3, 50, "grand legions", false, "Bob, Jo*", "ada1");

        // Act
        string asked = query.QueryString();

        // Assert
        Assert.Equal(
            "?game=grand%20legions&ruleset=a%26b&open=true&peopleOnly=false&over=true&preview=false"
            + "&players=Bob%2C%20Jo%2A&player=ada1&page=3&pageSize=50",
            asked
        );
    }
}
