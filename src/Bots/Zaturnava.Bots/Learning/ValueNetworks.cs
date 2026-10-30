using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;

namespace Net.NowhereAtAll.Zaturnava.Bots.Learning;

/// <summary>
/// The networks trained for each ruleset, by its id, kept in this assembly (one JSON file for each, in
/// <c>Networks/</c>) as <c>Zaturnava.BotRunner --learn</c> writes them; read the first time one is wanted.
/// </summary>
public static class ValueNetworks
{
    private const string Folder = "Networks/";

    private static readonly Lazy<ImmutableDictionary<string, ValueNetworkData>> Read = new(() =>
        typeof(ValueNetworks).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(Folder, StringComparison.Ordinal))
            .ToImmutableDictionary(
                name => Path.GetFileNameWithoutExtension(name[Folder.Length..]),
                name => ReadFrom(typeof(ValueNetworks).Assembly, name)
            )
    );

    public static ImmutableDictionary<string, ValueNetworkData> Learned => Read.Value;

    private static ValueNetworkData ReadFrom(Assembly assembly, string name)
    {
        using Stream stream = assembly.GetManifestResourceStream(name)!; // listed by the assembly itself
        return JsonSerializer.Deserialize<ValueNetworkData>(stream, JsonSerializerOptions.Web)!; // ours, so whole
    }
}
