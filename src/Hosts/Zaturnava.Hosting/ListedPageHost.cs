using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Hosting;

/// <summary>
/// A page served from origins named outright, such as a development server on this machine, or
/// any host that needs no adapter of its own.
/// </summary>
public sealed class ListedPageHost([Required] ImmutableList<string> origins) : IPageHosting
{
    public ImmutableList<string> Origins { get; } = origins;
}
