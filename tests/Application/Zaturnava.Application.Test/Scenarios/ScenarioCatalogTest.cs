using Net.NowhereAtAll.Zaturnava.Application.Scenarios;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Scenarios;

public sealed class ScenarioCatalogTest
{
    private readonly ScenarioCatalog _catalog = new(
        [
            new Scenario("tiny", "Second", "") { Id = "second" },
            new Scenario("tiny", "First", "") { Id = "first" },
        ]
    );

    [Fact]
    public void All_WhenAsked_ListsThemInOrderOfId()
    {
        // Arrange - two scenarios, out of order

        // Act
        IEnumerable<string> ids = this._catalog.All.Select(scenario => scenario.Id);

        // Assert
        Assert.Equal(["first", "second"], ids);
    }

    [Fact]
    public void Find_WhenThereIsSuchAScenario_FindsIt()
    {
        // Arrange - two scenarios

        // Act
        Scenario found = this._catalog.Find("second");

        // Assert
        Assert.Equal("Second", found.Name);
    }

    [Fact]
    public void Find_WhenThereIsNoSuchScenario_ThrowsUnknownScenarioException()
    {
        // Arrange - the catalog with nothing in it

        // Act
        UnknownScenarioException exception =
            Assert.Throws<UnknownScenarioException>(() => ScenarioCatalog.None.Find("second"));

        // Assert
        Assert.Equal("There is no 'second' scenario.", exception.Message);
    }
}
