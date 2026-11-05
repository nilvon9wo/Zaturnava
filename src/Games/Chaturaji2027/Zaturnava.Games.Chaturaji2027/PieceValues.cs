using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What each piece of a game is worth: what taking it scores (rules.md §5), and, where armies are bought, its price.
/// Read from a table of the game's own, not written into the program: <c>Values/{game}.json</c>, each piece's key
/// and its value, kept in the assembly that plays the game, which can be changed alone.
/// </summary>
public static class PieceValues
{
    private const string Folder = "Values/";

    /// <summary>What <paramref name="game"/>'s pieces are worth, as its table in the assembly says.</summary>
    /// <exception cref="ArgumentException">The assembly keeps no table for the game.</exception>
    public static ImmutableDictionary<PieceKindId, int> Of([Required] Assembly assembly, [Required] string game)
    {
        using Stream stream = assembly.GetManifestResourceStream($"{Folder}{game}.json")
            ?? throw new ArgumentException($"No table of piece values for {game}.", nameof(game));
        Dictionary<string, int> values = JsonSerializer.Deserialize<Dictionary<string, int>>(stream)!; // ours
        return values.ToImmutableDictionary(value => new PieceKindId(value.Key), value => value.Value);
    }
}
