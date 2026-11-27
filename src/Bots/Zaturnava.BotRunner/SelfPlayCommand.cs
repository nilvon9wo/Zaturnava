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
using Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

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

    private static readonly RulesetCatalog Rulesets = new([new Chaturaji2027Rules(), .. GrandLegionsRules.Quick]);

    private static readonly ImmutableList<IBotPlaying> Bots =
    [
        new AthenaBot(Random.Shared),
        new PythiaBot(Random.Shared),
        new TacticianBot(Random.Shared),
        new SageBot(Random.Shared),
        .. FamilyBot.All(Random.Shared),
        new ArgusBot(Random.Shared),
        new CroesusBot(Random.Shared),
        new MidasBot(Random.Shared),
        new SolomonBot(Random.Shared),
        NoviceBot.Icarus(Random.Shared),
        NoviceBot.Echo(Random.Shared),
        .. NoviceBot.Faint(Random.Shared),
        StumblerBot.Epimetheus(Random.Shared),
        StumblerBot.Narcissus(Random.Shared),
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
        await ((options.IsListing, options.TrainingRounds, options.LearningFrom) switch
        {
            (true, _, _) => List(rules, output),
            (_, { } rounds, _) => Train(rules, options, rounds, output, standardError, cancellationToken),
            (_, _, { } files) => Learn(rules, options, files, start, output, standardError, cancellationToken),
            _ => play.Run(options.Games, options.MaximumSteps, output, cancellationToken),
        });
        return Succeeded;
    }

    /// <summary>
    /// Writes the id of each of our bots that plays the ruleset (<see cref="IBotPlaying.Knows"/>), one to a line: a
    /// family's bot only where its pieces are to be had (2026-11-20: so that the tournaments rate them too).
    /// </summary>
    private static async Task List(IRulesApplying rules, TextWriter output)
    {
        foreach (IBotPlaying bot in Bots.Where(bot => bot.Knows(rules)))
        {
            await output.WriteLineAsync(bot.Profile.Id);
        }
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
        return new SelfPlay(
            rules,
            botIds,
            [.. botIds.Select(bots.Find)],
            new RandomDice(Random.Shared),
            options.Seed,
            options.StartsPath is { } starts ? StartsIn(starts, rules, options.Pieces) : null
        );
    }

    /// <summary>Where the games in the file at <paramref name="path"/> come down to so few pieces.</summary>
    /// <exception cref="ArgumentException">The file cannot be read, or none of its games does.</exception>
    private static ImmutableList<Played> StartsIn(string path, IRulesApplying rules, int pieces)
    {
        ImmutableList<Played> starts;
        try
        {
            starts = Starts.From(rules, File.ReadAllText(path), pieces);
        }
        catch (Exception unread) when (unread is IOException or JsonException)
        {
            throw new ArgumentException($"There are no games to start from in {path}: {unread.Message}", unread);
        }

        return starts.IsEmpty
            ? throw new ArgumentException($"No {rules.Id} game in {path} comes down to {pieces} pieces.")
            : starts;
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
            && PositionEncoder.Fitting(rules, network.Inputs) is not null
                ? network
                : throw new ArgumentException($"{path} holds no network judging {rules.Id}'s positions.");
    }

    private static int SeatCount(IRulesApplying rules) => rules.CreateInitialState().Seats.Count;

    /// <summary>The file to write, or standard output, left open for its owner to close.</summary>
    private static TextWriter Output(string? path, TextWriter standardOutput) =>
        path is null ? new NonClosingWriter(standardOutput) : File.CreateText(path);
}
