using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Bots of one's own, from an assembly built against <c>Zaturnava.Engine</c>: every public, concrete
/// <see cref="IBotPlaying"/> in it, made with a <see cref="Random"/> if it takes one, else with nothing.
/// One that takes neither is passed over.
/// </summary>
internal static class BotAssembly
{
    /// <exception cref="ArgumentException">
    /// There is no assembly at <paramref name="path"/>, or it is not one.
    /// </exception>
    public static ImmutableList<IBotPlaying> BotsIn([Required] string path, [Required] Random random) =>
        [
            .. Load(path)
                .GetExportedTypes()
                .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IBotPlaying)))
                .Select(type => Made(type, random))
                .OfType<IBotPlaying>(),
        ];

    private static Assembly Load(string path)
    {
        try
        {
            return Assembly.LoadFrom(Path.GetFullPath(path));
        }
        catch (Exception failure) when (failure is FileNotFoundException or BadImageFormatException)
        {
            throw new ArgumentException($"No bots could be loaded from '{path}': {failure.Message}", failure);
        }
    }

    private static IBotPlaying? Made(Type type, Random random) =>
        (type.GetConstructor([typeof(Random)]), type.GetConstructor(Type.EmptyTypes)) switch
        {
            ({ } seeded, _) => (IBotPlaying)seeded.Invoke([random]),
            (_, { } plain) => (IBotPlaying)plain.Invoke([]),
            _ => null,
        };
}
