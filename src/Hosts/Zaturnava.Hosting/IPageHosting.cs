using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Hosting;

/// <summary>
/// Wherever the web client's page is served from. The server lets a browser call it from these
/// origins, and from no others.
/// </summary>
public interface IPageHosting
{
    /// <summary>The page's origins: scheme, host and port, such as <c>https://example.com</c>.</summary>
    ImmutableList<string> Origins { get; }
}
