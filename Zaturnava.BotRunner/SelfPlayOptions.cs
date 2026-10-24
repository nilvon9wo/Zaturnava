using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// What to play: <c>--ruleset</c> (default classic), <c>--bots</c> (one id per seat, comma-separated;
/// default every seat random), <c>--games</c> (default 100), <c>--max-steps</c> per game (default
/// 1000), and <c>--out</c>, the file to write (default standard output).
/// </summary>
internal sealed record SelfPlayOptions(
    string RulesetId,
    ImmutableList<string>? Bots,
    int Games,
    int MaximumSteps,
    string? OutputPath
)
{
    public const string Usage =
        "Usage: Zaturnava.BotRunner [--ruleset classic] [--bots random,random,random,random] [--games 100] "
        + "[--max-steps 1000] [--out games.jsonl]";

    private static readonly SelfPlayOptions Defaults = new("classic", null, 100, 1000, null);

    /// <exception cref="ArgumentException">The arguments are not options this command takes.</exception>
    public static SelfPlayOptions Parse([Required] IReadOnlyList<string> args) =>
        args.Chunk(2).Aggregate(Defaults, (options, pair) => options.With(pair));

    private SelfPlayOptions With(string[] pair) =>
        pair.Length == 2
            ? this.With(pair[0], pair[1])
            : throw new ArgumentException($"{pair[0]} needs a value. {Usage}");

    private SelfPlayOptions With(string name, string value) =>
        name switch
        {
            "--ruleset" => this with { RulesetId = value },
            "--bots" => this with { Bots = [.. value.Split(',', StringSplitOptions.TrimEntries)] },
            "--games" => this with { Games = Count(name, value) },
            "--max-steps" => this with { MaximumSteps = Count(name, value) },
            "--out" => this with { OutputPath = value },
            _ => throw new ArgumentException($"There is no {name} option. {Usage}"),
        };

    private static int Count(string name, string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count) && count > 0
            ? count
            : throw new ArgumentException($"{name} must be a whole number above 0, not '{value}'. {Usage}");
}
