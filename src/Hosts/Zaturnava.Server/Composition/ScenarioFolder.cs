using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// The scenarios in a folder, and those below it: one to each <c>.json</c> file, its id the file's name.
/// The files are read whenever asked, so that one written or changed is offered at once. One that cannot be
/// read is offered all the same, for no ruleset, saying why, so that whoever wrote it sees what to fix.
/// </summary>
internal sealed class ScenarioFolder(string folder) : IScenarioLocating
{
    private readonly string _folder = folder;

    public ImmutableList<Scenario> All =>
        Directory.Exists(this._folder)
            ? new ScenarioCatalog(
                [.. Directory.EnumerateFiles(this._folder, "*.json", SearchOption.AllDirectories).Select(Read)]
            ).All
            : [];

    public Scenario Find([Required] string id) => new ScenarioCatalog(this.All).Find(id);

    private static Scenario Read(string file)
    {
        string id = Path.GetFileNameWithoutExtension(file);
        try
        {
            Scenario scenario = JsonSerializer.Deserialize<Scenario>(File.ReadAllText(file), JsonSerializerOptions.Web)
                ?? throw new JsonException("It is empty.");
            return scenario with { Id = id };
        }
        catch (JsonException exception)
        {
            return new Scenario(string.Empty, id, $"{Path.GetFileName(file)} cannot be read: {exception.Message}")
            {
                Id = id,
            };
        }
    }
}
