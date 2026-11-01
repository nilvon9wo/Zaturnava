using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// The links a player shows on their profile (<see cref="GameRoutes.Links"/>): web addresses, http or https, at
/// most <see cref="MaximumLinks"/>, each at most <see cref="MaximumLinkLength"/> characters.
/// </summary>
public sealed record LinksRequest([Required] ImmutableList<string> Links)
{
    public const int MaximumLinks = 5;

    public const int MaximumLinkLength = 200;
}
