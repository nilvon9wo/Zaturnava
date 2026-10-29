using System.Collections.Immutable;
using System.Reflection;
using System.Xml.Linq;

namespace Net.NowhereAtAll.Zaturnava.Architecture.Test;

/// <summary>
/// Which Zaturnava projects each one may use. Every service, client, board and piece set must stay
/// replaceable, so each project may reach only what it is built on; a project not listed here fails
/// until someone decides where it belongs.
/// </summary>
public sealed class DependencyRulesTest
{
    private const string Prefix = "Net.NowhereAtAll.Zaturnava.";
    private const string SolutionPrefix = "Zaturnava.";
    private const string SolutionName = "Zaturnava.slnx";

    private static readonly ImmutableDictionary<string, ImmutableHashSet<string>> MayUse =
        new Dictionary<string, ImmutableHashSet<string>>
        {
            // the kernel: rules as pure functions over abstract cells, pieces and seats
            ["Engine"] = [],

            // one board, one variant, one kind of player: each a plug-in to the engine
            ["Topology.SquareGrid"] = ["Engine"],
            ["Games.Chaturaji2027"] = ["Engine", "Topology.SquareGrid"],
            ["Bots"] = ["Engine"],
            ["Bots.OpenAiCompatible"] = ["Bots", "Engine"],

            // the use cases, and the adapters plugged into their ports
            ["Application"] = ["Engine"],
            ["Persistence.InMemory"] = ["Application", "Engine"],
            ["Persistence.EfCore"] = ["Application", "Engine"],

            // the wire protocol, and what a client needs of it: nothing of the server
            ["Contracts"] = [],
            ["Client"] = ["Contracts"],
            ["Presentation"] = ["Contracts"],
            ["Web"] = ["Client", "Contracts", "Presentation"],
            ["Mcp"] = ["Client", "Contracts"],

            // the services around the game, each vendor in an adapter of its own
            ["Hosting"] = [],
            ["Hosting.Itch"] = ["Hosting"],

            // self-play: the application's decision loop, with the server's rulesets and bots
            ["BotRunner"] = ["Application", "Bots", "Engine", "Games.Chaturaji2027"],

            // the composition root: the one project that knows everything
            ["Server"] =
            [
                "Application", "Bots", "Bots.OpenAiCompatible", "Contracts", "Engine", "Hosting", "Hosting.Itch",
                "Persistence.EfCore", "Persistence.InMemory", "Topology.SquareGrid", "Games.Chaturaji2027",
            ],
        }.ToImmutableDictionary();

    public static TheoryData<string> Projects => [.. MayUse.Keys.Order()];

    [Theory]
    [MemberData(nameof(Projects))]
    public void Project_WhenBuilt_UsesOnlyTheProjectsItMay(string project)
    {
        // Arrange
        Assembly assembly = Assembly.Load(Prefix + project);

        // Act
        ImmutableSortedSet<string> used = [.. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name => name[Prefix.Length..])];

        // Assert
        Assert.Empty(used.Except(MayUse[project]));
    }

    [Fact]
    public void Rules_WhenTheSolutionGrows_CoverEveryProject()
    {
        // Arrange
        XDocument solution = XDocument.Load(SolutionFile(new DirectoryInfo(AppContext.BaseDirectory)));

        // Act
        ImmutableList<string> projects = [.. solution.Descendants("Project")
            .Select(project => Path.GetFileNameWithoutExtension((string?)project.Attribute("Path") ?? string.Empty))
            .Where(name => name.StartsWith(SolutionPrefix, StringComparison.Ordinal))
            .Select(name => name[SolutionPrefix.Length..])
            .Where(project => !project.EndsWith(".Test", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        // Assert
        Assert.Equal(MayUse.Keys.Order(StringComparer.Ordinal), projects);
    }

    /// <summary>Zaturnava.slnx, in the nearest directory above the tests that has it.</summary>
    private static string SolutionFile(DirectoryInfo directory) =>
        Path.Combine(directory.FullName, SolutionName) is { } path && File.Exists(path)
            ? path
            : SolutionFile(directory.Parent ?? throw new FileNotFoundException(SolutionName));
}
