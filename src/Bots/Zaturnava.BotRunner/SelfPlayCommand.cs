using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// The command line: which rulesets and bots exist (the same ones the server hosts), what the
/// options choose among them, and where the games go. Returns the process's exit code.
/// </summary>
internal static class SelfPlayCommand
{
    public const int Succeeded = 0;
    public const int Misused = 2;

    private static readonly JsonSerializerOptions Indented = new(JsonSerializerOptions.Web) { WriteIndented = true };

    private static readonly RulesetCatalog Rulesets = new([new Chaturaji2027Rules()]);

    private static readonly ImmutableList<IBotPlaying> Bots =
    [
        new SageBot(Random.Shared),
        new ArgusBot(Random.Shared),
        new MidasBot(Random.Shared),
        new SolomonBot(Random.Shared),
        new RandomBot(Random.Shared),
    ];

    public static async Task<int> Run(
        [Required] IReadOnlyList<string> args,
        [Required] TextWriter standardOutput,
        [Required] TextWriter standardError,
        CancellationToken cancellationToken
    )
    {
        SelfPlayOptions options;
        IRulesApplying rules;
        SelfPlay play;
        try
        {
            options = SelfPlayOptions.Parse(args);
            rules = Rulesets.Find(options.RulesetId);
            play = Prepare(options, rules);
        }
        catch (Exception misuse) when (misuse is ArgumentException or GameRequestException)
        {
            await standardError.WriteLineAsync(misuse.Message);
            return Misused;
        }

        await using TextWriter output = Output(options.OutputPath, standardOutput);
        await (options.TrainingRounds is { } rounds
            ? Train(rules, options, rounds, output, standardError, cancellationToken)
            : play.Run(options.Games, options.MaximumSteps, output, cancellationToken));
        return Succeeded;
    }

    /// <summary>
    /// Tunes Sage's weights for the ruleset, from what it has learned so far, telling how each nudge went on
    /// <paramref name="progress"/>, and writes the weights tuned, as JSON for <c>Zaturnava.Bots/Weights</c>.
    /// </summary>
    private static async Task Train(
        IRulesApplying rules,
        SelfPlayOptions options,
        int rounds,
        TextWriter output,
        TextWriter progress,
        CancellationToken cancellationToken
    )
    {
        SageMatch match = new(rules, new RandomDice(Random.Shared), options.Games, options.MaximumSteps);
        SageWeights start = SageWeights.Learned.GetValueOrDefault(rules.Id, SageWeights.Default);
        SageWeights tuned = await new Training(match.Play, progress).Tune(start, rounds, cancellationToken);
        await output.WriteLineAsync(JsonSerializer.Serialize(tuned, Indented));
    }

    /// <exception cref="GameRequestException">The ruleset or a bot does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// The bot assembly cannot be loaded, or one of its bots has the id of another.
    /// </exception>
    private static SelfPlay Prepare(SelfPlayOptions options, IRulesApplying rules)
    {
        BotCatalog bots = new(
            options.BotAssemblyPath is { } path
                ? [.. Bots, .. BotAssembly.BotsIn(path, Random.Shared)]
                : Bots
        );
        ImmutableList<string> botIds = options.Bots ?? [.. Enumerable.Repeat(RandomBot.BotId, SeatCount(rules))];
        return new SelfPlay(rules, botIds, [.. botIds.Select(bots.Find)], new RandomDice(Random.Shared));
    }

    private static int SeatCount(IRulesApplying rules) => rules.CreateInitialState().Seats.Count;

    /// <summary>The file to write, or standard output, left open for its owner to close.</summary>
    private static TextWriter Output(string? path, TextWriter standardOutput) =>
        path is null ? new NonClosingWriter(standardOutput) : File.CreateText(path);
}
