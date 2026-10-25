using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// An action as plain data: a kind and named arguments. Clients echo back one of a game's legal
/// actions; they never need to know what its kind means. Both are required (no arguments is an
/// empty dictionary). A legal action the server can say more about than its kind carries a
/// <see cref="Label"/> for its button; the server ignores it on the way back, and an action without
/// one is sent without one.
/// </summary>
public sealed record ActionDto(
    [Required] string Kind,
    [Required] ImmutableSortedDictionary<string, string> Arguments
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; init; }
}
