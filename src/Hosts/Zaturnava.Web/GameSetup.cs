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
    private const string OpenerKey = "opener";
    private const string TerrainKey = "terrain";
    private const string LeadersKey = "leaders";
    private const string TurnTimeKey = "turn-time";
    private const string StartRowsKey = "start-rows";
    private const string FaceOffPauseKey = "face-off-pause";

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

    /// <summary>Without dice, the seat chosen to open the game; none, for one drawn at random.</summary>
    public int? Opener { get; init; }

    /// <summary>Whether a team with no leader left on the board has lost.</summary>
    public bool IsLostWithoutLeaders { get; init; }

    /// <summary>How many seconds each player may be idle on their turn before it passes; none, no limit.</summary>
    public int? IdleSeconds { get; init; }

    /// <summary>How many of each army's rows are its start rows; the game's own, if none.</summary>
    public int? StartRows { get; init; }

    /// <summary>How many seconds the game pauses when a person first may call a face-off; none, no pause.</summary>
    public int? FaceOffPause { get; init; }

    /// <summary>The board's size, for a game whose creator may set it; the game's own, if none.</summary>
    public BoardSizeDto? Board { get; init; }

    /// <summary>The bits of the terrain chosen, for a game whose creator may choose it; 0, for all dealt.</summary>
    public int TerrainMask { get; init; }

    /// <summary>How the bits of <see cref="TerrainMask"/> were set.</summary>
    public int TerrainValue { get; init; }

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
            this.Opener is { } opener ? $"{OpenerKey}={opener.ToString(CultureInfo.InvariantCulture)}" : null,
            this.IsLostWithoutLeaders ? $"{LeadersKey}=1" : null,
            this.IdleSeconds is { } seconds
                ? $"{TurnTimeKey}={seconds.ToString(CultureInfo.InvariantCulture)}"
                : null,
            this.StartRows is { } rows ? $"{StartRowsKey}={rows.ToString(CultureInfo.InvariantCulture)}" : null,
            this.FaceOffPause is { } pause
                ? $"{FaceOffPauseKey}={pause.ToString(CultureInfo.InvariantCulture)}"
                : null,
            this.TerrainMask is not 0
                ? string.Create(CultureInfo.InvariantCulture, $"{TerrainKey}={this.TerrainMask}:{this.TerrainValue}")
                : null,
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
                Opener = choices.TryGetValue(OpenerKey, out string? opener) && Number(opener) is >= 0 and var seat
                    ? seat
                    : null,
                IsLostWithoutLeaders = choices.ContainsKey(LeadersKey),
                IdleSeconds = choices.TryGetValue(TurnTimeKey, out string? time) && Number(time) is > 0 and var seconds
                    ? seconds
                    : null,
                StartRows = choices.TryGetValue(StartRowsKey, out string? start) && Number(start) is > 0 and var rows
                    ? rows
                    : null,
                FaceOffPause =
                    choices.TryGetValue(FaceOffPauseKey, out string? pause) && Number(pause) is > 0 and var paused
                        ? paused
                        : null,
                Board = choices.TryGetValue(BoardKey, out string? board) ? BoardOf(board) : null,
                TerrainMask = TerrainOf(choices).Mask,
                TerrainValue = TerrainOf(choices).Value,
            }
            : null;
    }

    /// <summary>The terrain "6:4" keeps, its bits chosen and how they were set; else none chosen.</summary>
    private static (int Mask, int Value) TerrainOf(ImmutableDictionary<string, string> choices)
    {
        ImmutableList<int?> bits = choices.TryGetValue(TerrainKey, out string? terrain)
            ? [.. terrain.Split(':').Select(Number)]
            : [];
        return bits is [{ } mask, { } value] ? (mask, value) : (0, 0);
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
