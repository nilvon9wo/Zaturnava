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
        GameListQuery query = new("a&b", true, false, true, 3, 50, "grand legions");

        // Act
        string asked = query.QueryString();

        // Assert
        Assert.Equal(
            "?game=grand%20legions&ruleset=a%26b&open=true&peopleOnly=false&over=true&page=3&pageSize=50",
            asked
        );
    }
}
