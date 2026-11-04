using System.Collections.Immutable;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How the player last set a game up, to set it up so again: its size (the ruleset), their colour, and each seat's
/// strength (<see cref="GameStarter.PersonLevel"/> and the like included) and bot chosen (empty, for any).
/// </summary>
internal sealed record GameSetup(string RulesetId, int Colour, ImmutableList<int> Levels, ImmutableList<string> Picks)
{
    /// <summary>As the browser keeps it: "chaturaji-2027;0;-1,1,1,1;,athena,,".</summary>
    public string Text =>
        string.Join(
            ';',
            this.RulesetId,
            this.Colour.ToString(CultureInfo.InvariantCulture),
            string.Join(',', this.Levels.Select(level => level.ToString(CultureInfo.InvariantCulture))),
            string.Join(',', this.Picks)
        );

    /// <summary>The setup <paramref name="text"/> keeps; none, if it keeps none, or not one this can read.</summary>
    public static GameSetup? Parse(string? text)
    {
        string[] parts = text?.Split(';') ?? [];
        ImmutableList<int?> levels = parts.Length == 4 ? [.. parts[2].Split(',').Select(Number)] : [];
        return parts.Length == 4 && Number(parts[1]) is { } colour && levels.All(level => level is not null)
            ? new GameSetup(
                parts[0],
                colour,
                [.. levels.Select(level => level.GetValueOrDefault())],
                [.. parts[3].Split(',')]
            )
            : null;
    }

    private static int? Number(string text) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
            ? number
            : null;
}
