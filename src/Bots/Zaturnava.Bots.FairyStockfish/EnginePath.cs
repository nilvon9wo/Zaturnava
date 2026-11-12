using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish;

/// <summary>
/// Where Fairy-Stockfish's program is: where <see cref="Variable"/> says; else the first file, by name, whose name
/// starts <c>fairy-stockfish</c>, in <see cref="Folder"/> or any folder in it, of the nearest folder up from where
/// it starts looking that has one; else none.
/// </summary>
public static class EnginePath
{
    public const string Variable = "FAIRY_STOCKFISH_PATH";

    /// <summary>The folder it is looked for in, from the repository's root: kept out of the repository.</summary>
    public static readonly string Folder = Path.Combine("artifacts", "fairy-stockfish");

    private const string Prefix = "fairy-stockfish";

    public static string? Find(string? variable, [Required] DirectoryInfo start) =>
        variable ?? Within(start);

    private static string? Within(DirectoryInfo? folder) =>
        folder is null
            ? null
            : Directory.Exists(Path.Combine(folder.FullName, Folder))
                && Directory
                    .EnumerateFiles(Path.Combine(folder.FullName, Folder), Prefix + "*", SearchOption.AllDirectories)
                    .Order(StringComparer.Ordinal)
                    .FirstOrDefault() is { } found
                ? found
                : Within(folder.Parent);
}
