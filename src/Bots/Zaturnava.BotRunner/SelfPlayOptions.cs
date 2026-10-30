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
/// <c>--bot-assembly</c> names an assembly of one's own bots (<see cref="BotAssembly"/>), to seat by their
/// ids beside ours; <c>--network</c>, a network's file for Athena to play by, rather than the one she was
/// built with. With <c>--learn</c>, the files of game histories to learn from (comma-separated), it trains
/// a network to judge the ruleset's positions instead (<see cref="Learning"/>), and writes it: starting from
/// the network in the file <c>--from</c> names, or else from one judging as Sage does, with <c>--hidden</c>
/// units (default 32); in <c>--epochs</c> rounds (default 8), each position taught how the game went by
/// <c>--lambda</c> (default 1) and what follows it by the rest, in steps of <c>--rate</c> (default 0.1),
/// shared out over how big each position's numbers are.
/// </summary>
internal sealed record SelfPlayOptions(
    string RulesetId,
    ImmutableList<string>? Bots,
    int Games,
    int MaximumSteps,
    string? OutputPath,
    int? TrainingRounds = null,
    string? BotAssemblyPath = null,
    ImmutableList<string>? LearningFrom = null,
    int Epochs = 8,
    int Hidden = 32,
    string? NetworkPath = null,
    string? StartPath = null,
    double Lambda = 1,
    double Rate = 0.1
)
{
    public const string Usage =
        "Usage: Zaturnava.BotRunner [--ruleset chaturaji-2027] [--bots random,random,random,random] [--games 100] "
        + "[--max-steps 1000] [--out games.jsonl] [--train rounds] [--bot-assembly MyBots.dll] "
        + "[--network athena.json] [--learn games.jsonl,more.jsonl [--from athena.json] [--hidden 32] "
        + "[--epochs 8] [--lambda 1] [--rate 0.1]]";

    private static readonly SelfPlayOptions Defaults = new("chaturaji-2027", null, 100, 1000, null);

    /// <summary>How each position is to be learned from.</summary>
    public LearningPace Pace => new(this.Epochs, this.Lambda, this.Rate);

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
            "--bot-assembly" => this with { BotAssemblyPath = value },
            "--network" => this with { NetworkPath = value },
            "--learn" => this with { LearningFrom = [.. value.Split(',', StringSplitOptions.TrimEntries)] },
            "--from" => this with { StartPath = value },
            "--epochs" => this with { Epochs = Count(name, value) },
            "--hidden" => this with { Hidden = Count(name, value) },
            "--lambda" => this with { Lambda = Share(name, value) },
            "--rate" => this with { Rate = Share(name, value) },
            _ => throw new ArgumentException($"There is no {name} option. {Usage}"),
        };

    private static int Count(string name, string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count) && count > 0
            ? count
            : throw new ArgumentException($"{name} must be a whole number above 0, not '{value}'. {Usage}");

    private static double Share(string name, string value) =>
        double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double share)
            && share <= 1
            ? share
            : throw new ArgumentException($"{name} must be a number from 0 to 1, not '{value}'. {Usage}");
}
