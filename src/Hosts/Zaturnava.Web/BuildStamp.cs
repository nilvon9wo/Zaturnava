using System.Reflection;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Which build of the page this is, for the bar to show, so that an old copy kept anywhere shows.</summary>
internal static class BuildStamp
{
    private const int ShortCommit = 7;

    /// <summary>This page's build, as <see cref="Of"/> tells it.</summary>
    public static string Current =>
        Of(typeof(BuildStamp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>());

    /// <summary>
    /// "Build 1a2b3c4": the commit an informational version ("0.0.0-dev+1a2b3c4d...") names, shortened; else
    /// "Build 0.0.0-dev", the version itself; "Build unknown" without one.
    /// </summary>
    public static string Of(AssemblyInformationalVersionAttribute? version)
    {
        string[] parts = (version?.InformationalVersion ?? "unknown").Split('+');
        string build = parts.Length > 1 ? parts[1][..Math.Min(ShortCommit, parts[1].Length)] : parts[0];
        return $"Build {build}";
    }
}
