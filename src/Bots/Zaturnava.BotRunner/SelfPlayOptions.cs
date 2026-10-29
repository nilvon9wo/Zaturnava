using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// What to play: <c>--ruleset</c> (default chaturaji-2027), <c>--bots</c> (one id per seat, comma-separated;
/// default every seat random), <c>--games</c> (default 100), <c>--max-steps</c> per game (default
/// 1000), and <c>--out</c>, the file to write (default standard output). With <c>--train</c>, the number of
/// rounds, it tunes Sage's weights for the ruleset instead (<see cref="Training"/>), each nudge tried over
/// <c>--games</c> games with each set of weights on each side, and writes the weights tuned.
/// </summary>
internal sealed record SelfPlayOptions(
    string RulesetId,
    ImmutableList<string>? Bots,
    int Games,
    int MaximumSteps,
    string? OutputPath,
    int? TrainingRounds = null
)
{
    public const string Usage =
        "Usage: Zaturnava.BotRunner [--ruleset chaturaji-2027] [--bots random,random,random,random] [--games 100] "
        + "[--max-steps 1000] [--out games.jsonl] [--train rounds]";

    private static readonly SelfPlayOptions Defaults = new("chaturaji-2027", null, 100, 1000, null);

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
            "--train" => this with { TrainingRounds = Count(name, value) },
            _ => throw new ArgumentException($"There is no {name} option. {Usage}"),
        };

    private static int Count(string name, string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count) && count > 0
            ? count
            : throw new ArgumentException($"{name} must be a whole number above 0, not '{value}'. {Usage}");
}
