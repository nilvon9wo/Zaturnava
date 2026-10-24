using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// An action as plain data: a kind and named arguments. Clients echo back one of a game's legal
/// actions; they never need to know what its kind means. Both are required (no arguments is an
/// empty dictionary).
/// </summary>
public sealed record ActionDto(
    [Required] string Kind,
    [Required] ImmutableSortedDictionary<string, string> Arguments
);
