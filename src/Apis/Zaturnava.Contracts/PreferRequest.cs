using System.ComponentModel.DataAnnotations;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// One of the page's settings for the player to keep (<see cref="GameRoutes.Preferences"/>): its name and value, an
/// empty value forgetting it. A name and a value are each at most 64 characters, and a player keeps at most 32.
/// </summary>
public sealed record PreferRequest(
    [Required(AllowEmptyStrings = true)] string Name,
    [Required(AllowEmptyStrings = true)] string Value
);
