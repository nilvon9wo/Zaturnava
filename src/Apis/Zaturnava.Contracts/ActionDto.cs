using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// An action as plain data: a kind and named arguments. Clients echo back one of a game's legal
/// actions; they never need to know what its kind means. Both are required (no arguments is an
/// empty dictionary). A legal action the server can say more about than its kind carries a
/// <see cref="Label"/> for its button; one chosen in two steps, as an escape is, also a <see cref="Choice"/>.
/// The server ignores both on the way back, and an action without one is sent without it.
/// </summary>
public sealed record ActionDto(
    [Required] string Kind,
    [Required] ImmutableSortedDictionary<string, string> Arguments
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; init; }

    /// <summary>
    /// What the actions alike but for their <see cref="ActionArguments.Home"/> are chosen by first, once for them
    /// all ("Escape Black's Boat, held by Yellow"); the cell then chooses among them.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Choice { get; init; }
}
