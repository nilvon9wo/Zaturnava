using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

public sealed class ScenarioFolderTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("scenarios-").FullName;

    public void Dispose() => Directory.Delete(this._folder, recursive: true);

    [Fact]
    public void All_WhenTheFolderHasScenarios_ReadsEachBelowItByItsFileName()
    {
        // Arrange - one beside the other, and one in a folder below
        this.Write("b.json", """{ "rulesetId": "tiny", "name": "B", "description": "Second.", "turn": "Seat 1" }""");
        this.Write(Path.Combine("deeper", "a.json"), """{ "rulesetId": "tiny", "name": "A", "description": "" }""");
        ScenarioFolder folder = new(this._folder);

        // Act
        Scenario found = folder.Find("b");

        // Assert
        Assert.Equal(["a", "b"], folder.All.Select(scenario => scenario.Id));
        Assert.Equal(("tiny", "B", "Second.", "Seat 1"), (found.RulesetId, found.Name, found.Description, found.Turn));
    }

    [Theory]
    [InlineData("{ not json", "broken.json cannot be read: ")]
    [InlineData("null", "broken.json cannot be read: It is empty.")]
    public void All_WhenAFileCannotBeRead_OffersItForNoRulesetSayingWhy(string text, string expected)
    {
        // Arrange
        this.Write("broken.json", text);

        // Act
        Scenario broken = new ScenarioFolder(this._folder).All.Single();

        // Assert
        Assert.Equal((string.Empty, "broken"), (broken.RulesetId, broken.Name));
        Assert.StartsWith(expected, broken.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void All_WhenThereIsNoSuchFolder_OffersNone()
    {
        // Arrange
        ScenarioFolder folder = new(Path.Combine(this._folder, "missing"));

        // Act
        int count = folder.All.Count;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void AddZaturnava_WhenNoScenarioFolderIsConfigured_OffersNone()
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        IScenarioLocating scenarios = new ServiceCollection()
            .AddZaturnava(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IScenarioLocating>();

        // Assert
        Assert.Same(ScenarioCatalog.None, scenarios);
    }

    [Fact]
    public void AddZaturnava_WhenAScenarioFolderIsConfigured_OffersWhatIsInIt()
    {
        // Arrange - with no content root, the folder is found from where the process runs
        this.Write("a.json", """{ "rulesetId": "tiny", "name": "A", "description": "" }""");
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([KeyValuePair.Create<string, string?>("Scenarios:Folder", this._folder)])
            .Build();

        // Act
        IScenarioLocating scenarios = new ServiceCollection()
            .AddZaturnava(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IScenarioLocating>();

        // Assert
        Assert.Equal("a", scenarios.All.Single().Id);
    }

    private void Write(string file, string text)
    {
        string path = Path.Combine(this._folder, file);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
