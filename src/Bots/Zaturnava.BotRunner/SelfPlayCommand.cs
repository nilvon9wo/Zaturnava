using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
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
        new AthenaBot(Random.Shared),
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
        ValueNetwork? start;
        try
        {
            options = SelfPlayOptions.Parse(args);
            rules = Rulesets.Find(options.RulesetId);
            play = Prepare(options, rules);
            start = options.StartPath is { } path ? ValueNetwork.From(NetworkIn(path, rules)) : null;
        }
        catch (Exception misuse) when (misuse is ArgumentException or GameRequestException)
        {
            await standardError.WriteLineAsync(misuse.Message);
            return Misused;
        }

        await using TextWriter output = Output(options.OutputPath, standardOutput);
        await ((options.TrainingRounds, options.LearningFrom) switch
        {
            ({ } rounds, _) => Train(rules, options, rounds, output, standardError, cancellationToken),
            (_, { } files) => Learn(rules, options, files, start, output, standardError, cancellationToken),
            _ => play.Run(options.Games, options.MaximumSteps, output, cancellationToken),
        });
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
        SageWeights start = SageWeights.Learned
            .GetValueOrDefault(rules.Id, SageWeights.Default)
            .WeighingMeasuresOf(rules);
        SageWeights tuned = await new Training(match.Play, progress).Tune(start, rounds, cancellationToken);
        await output.WriteLineAsync(JsonSerializer.Serialize(tuned, Indented));
    }

    /// <summary>
    /// Trains a network to judge the ruleset's positions on the games in <paramref name="files"/>, from
    /// <paramref name="start"/> or else from one judging as Sage does, telling how each round went on
    /// <paramref name="progress"/>, and writes it, as JSON for <c>Zaturnava.Bots/Networks</c>.
    /// </summary>
    private static async Task Learn(
        IRulesApplying rules,
        SelfPlayOptions options,
        ImmutableList<string> files,
        ValueNetwork? start,
        TextWriter output,
        TextWriter progress,
        CancellationToken cancellationToken
    )
    {
        Learning learning = new(rules, progress);
        ValueNetworkData network = await learning.Learn(
            files.SelectMany(File.ReadLines),
            start ?? learning.Start(options.Hidden, Random.Shared),
            options.Pace,
            Random.Shared,
            cancellationToken
        );
        await output.WriteLineAsync(JsonSerializer.Serialize(network, Indented));
    }

    /// <exception cref="GameRequestException">The ruleset or a bot does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// The bot assembly cannot be loaded, or one of its bots has the id of another; or Athena's network cannot.
    /// </exception>
    private static SelfPlay Prepare(SelfPlayOptions options, IRulesApplying rules)
    {
        ImmutableList<IBotPlaying> ours = options.NetworkPath is { } network
            ? WithNetwork(NetworkIn(network, rules), rules)
            : Bots;
        BotCatalog bots = new(
            options.BotAssemblyPath is { } path
                ? [.. ours, .. BotAssembly.BotsIn(path, Random.Shared)]
                : ours
        );
        ImmutableList<string> botIds = options.Bots ?? [.. Enumerable.Repeat(RandomBot.BotId, SeatCount(rules))];
        return new SelfPlay(rules, botIds, [.. botIds.Select(bots.Find)], new RandomDice(Random.Shared));
    }

    /// <summary>Our bots, but Athena playing the ruleset by <paramref name="network"/>.</summary>
    private static ImmutableList<IBotPlaying> WithNetwork(ValueNetworkData network, IRulesApplying rules)
    {
        AthenaBot athena = new(
            Random.Shared,
            ImmutableDictionary.Create<string, ValueNetworkData>(StringComparer.Ordinal).Add(rules.Id, network)
        );
        return [.. Bots.Select(bot => bot is AthenaBot ? athena : bot)];
    }

    /// <summary>The network kept in the file at <paramref name="path"/>, for the ruleset's positions.</summary>
    /// <exception cref="ArgumentException">The file cannot be read, or holds no such network.</exception>
    private static ValueNetworkData NetworkIn(string path, IRulesApplying rules)
    {
        ValueNetworkData? data;
        try
        {
            data = JsonSerializer.Deserialize<ValueNetworkData>(File.ReadAllText(path), JsonSerializerOptions.Web);
        }
        catch (Exception unread) when (unread is IOException or JsonException)
        {
            throw new ArgumentException($"There is no network to read in {path}: {unread.Message}", unread);
        }

        return data is { } network
            && network.RulesetId == rules.Id
            && network.Inputs == new PositionEncoder(rules).Size
                ? network
                : throw new ArgumentException($"{path} holds no network judging {rules.Id}'s positions.");
    }

    private static int SeatCount(IRulesApplying rules) => rules.CreateInitialState().Seats.Count;

    /// <summary>The file to write, or standard output, left open for its owner to close.</summary>
    private static TextWriter Output(string? path, TextWriter standardOutput) =>
        path is null ? new NonClosingWriter(standardOutput) : File.CreateText(path);
}
