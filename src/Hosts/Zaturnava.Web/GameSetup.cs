using System.Collections.Immutable;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// How the player last set a game up, to set it up so again: its size (the ruleset), their colour, and each seat's
/// strength (<see cref="GameStarter.PersonLevel"/> and the like included) and bot chosen (empty, for any); and the
/// game's other choices (Stated, 2026-11-13: "like other settings, the system should remember how it was last set").
/// </summary>
internal sealed record GameSetup(string RulesetId, int Colour, ImmutableList<int> Levels, ImmutableList<string> Picks)
{
    private const string ArmiesKey = "armies";
    private const string BudgetKey = "budget";
    private const string SecretKey = "secret";
    private const string HiddenKey = "hidden";
    private const string PlayersKey = "players";
    private const string SideKey = "side";
    private const string BoardKey = "board";
    private const string DiceOffKey = "dice-off";
    private const string IdleKey = "idle";

    /// <summary>How the armies come to be, as the lobby names its choices; the game's default, if none.</summary>
    public string? Armies { get; init; }

    /// <summary>For armies drafted, what each has to spend; the game's own, if none.</summary>
    public int? Budget { get; init; }

    /// <summary>For armies each drafts their own, whether in secret.</summary>
    public bool IsSecret { get; init; }

    /// <summary>For armies drafted in secret, whether from allies too.</summary>
    public bool IsHiddenFromAllies { get; init; }

    /// <summary>Whether the open seats are for players signed in only.</summary>
    public bool IsForPlayers { get; init; }

    /// <summary>Whether pieces promoted at the far edge are promoted at the edge to their right too.</summary>
    public bool PromotesAtSide { get; init; }

    /// <summary>Whether the game is played without dice.</summary>
    public bool AreDiceOff { get; init; }

    /// <summary>How many idle rounds end the game; the server's own, if none.</summary>
    public int? IdleRounds { get; init; }

    /// <summary>The board's size, for a game whose creator may set it; the game's own, if none.</summary>
    public BoardSizeDto? Board { get; init; }

    /// <summary>
    /// As the browser keeps it: "chaturaji-2027;0;-1,1,1,1;,athena,,;armies=random-each,secret=1"; the last part, the
    /// other choices, only those made, and none at all in a setup kept before they were.
    /// </summary>
    public string Text =>
        string.Join(
            ';',
            new[]
            {
                this.RulesetId,
                this.Colour.ToString(CultureInfo.InvariantCulture),
                string.Join(',', this.Levels.Select(level => level.ToString(CultureInfo.InvariantCulture))),
                string.Join(',', this.Picks),
            }.Concat(this.Choices().Any() ? [string.Join(',', this.Choices())] : [])
        );

    private IEnumerable<string> Choices()
    {
        string?[] choices =
        [
            this.Armies is { } armies ? $"{ArmiesKey}={armies}" : null,
            this.Budget is { } budget ? $"{BudgetKey}={budget.ToString(CultureInfo.InvariantCulture)}" : null,
            this.IsSecret ? $"{SecretKey}=1" : null,
            this.IsHiddenFromAllies ? $"{HiddenKey}=1" : null,
            this.IsForPlayers ? $"{PlayersKey}=1" : null,
            this.PromotesAtSide ? $"{SideKey}=1" : null,
            this.AreDiceOff ? $"{DiceOffKey}=1" : null,
            this.IdleRounds is { } idle ? $"{IdleKey}={idle.ToString(CultureInfo.InvariantCulture)}" : null,
            this.Board is { } board
                ? $"{BoardKey}=" + string.Join(
                    'x',
                    new[] { board.Width, board.Height, board.KingdomWidth, board.KingdomDepth }.Select(size =>
                        size.ToString(CultureInfo.InvariantCulture)
                    )
                )
                : null,
        ];
        return choices.OfType<string>();
    }

    /// <summary>The setup <paramref name="text"/> keeps; none, if it keeps none, or not one this can read.</summary>
    public static GameSetup? Parse(string? text)
    {
        string[] parts = text?.Split(';') ?? [];
        bool isWhole = parts.Length is 4 or 5;
        ImmutableList<int?> levels = isWhole ? [.. parts[2].Split(',').Select(Number)] : [];
        ImmutableDictionary<string, string> choices = parts.Length == 5 ? ChoicesOf(parts[4]) : [];
        return isWhole && Number(parts[1]) is { } colour && levels.All(level => level is not null)
            ? new GameSetup(
                parts[0],
                colour,
                [.. levels.Select(level => level.GetValueOrDefault())],
                [.. parts[3].Split(',')]
            )
            {
                Armies = choices.GetValueOrDefault(ArmiesKey),
                Budget = choices.TryGetValue(BudgetKey, out string? budget) ? Number(budget) : null,
                IsSecret = choices.ContainsKey(SecretKey),
                IsHiddenFromAllies = choices.ContainsKey(HiddenKey),
                IsForPlayers = choices.ContainsKey(PlayersKey),
                PromotesAtSide = choices.ContainsKey(SideKey),
                AreDiceOff = choices.ContainsKey(DiceOffKey),
                IdleRounds = choices.TryGetValue(IdleKey, out string? idle) && Number(idle) is > 0 and var rounds
                    ? rounds
                    : null,
                Board = choices.TryGetValue(BoardKey, out string? board) ? BoardOf(board) : null,
            }
            : null;
    }

    /// <summary>The board "12x10x4x4" keeps, its width, height, and kingdoms' width and depth; else none.</summary>
    private static BoardSizeDto? BoardOf(string text)
    {
        ImmutableList<int?> sizes = [.. text.Split('x').Select(Number)];
        return sizes.Count == 4 && sizes.All(size => size > 0)
            ? new BoardSizeDto(sizes[0]!.Value, sizes[1]!.Value, sizes[2]!.Value, sizes[3]!.Value)
            : null;
    }

    /// <summary>The choices "armies=random-each,secret=1" keeps, by name; none not a name and a value.</summary>
    private static ImmutableDictionary<string, string> ChoicesOf(string text) =>
        text.Split(',')
            .Select(choice => choice.Split('='))
            .Where(choice => choice.Length == 2)
            .GroupBy(choice => choice[0], StringComparer.Ordinal)
            .ToImmutableDictionary(choice => choice.Key, choice => choice.First()[1], StringComparer.Ordinal);

    private static int? Number(string text) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
            ? number
            : null;
}
