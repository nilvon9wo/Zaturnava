using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Rulesets;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// The command line: which rulesets and bots exist (the same ones the server hosts), what the
/// options choose among them, and where the games go. Returns the process's exit code.
/// </summary>
internal static class SelfPlayCommand
{
    public const int Succeeded = 0;
    public const int Misused = 2;

    private static readonly RulesetCatalog Rulesets = new([new ClassicRules()]);

    private static readonly BotCatalog Bots =
        new(ImmutableDictionary<string, IDeciding>.Empty.Add(RandomBot.BotId, new RandomBot(Random.Shared)));

    public static async Task<int> Run(
        [Required] IReadOnlyList<string> args,
        [Required] TextWriter standardOutput,
        [Required] TextWriter standardError,
        CancellationToken cancellationToken
    )
    {
        SelfPlayOptions options;
        SelfPlay play;
        try
        {
            options = SelfPlayOptions.Parse(args);
            play = Prepare(options);
        }
        catch (Exception misuse) when (misuse is ArgumentException or GameRequestException)
        {
            await standardError.WriteLineAsync(misuse.Message);
            return Misused;
        }

        await using TextWriter output = Output(options.OutputPath, standardOutput);
        await play.Run(options.Games, options.MaximumSteps, output, cancellationToken);
        return Succeeded;
    }

    /// <exception cref="GameRequestException">The ruleset or a bot does not exist.</exception>
    private static SelfPlay Prepare(SelfPlayOptions options)
    {
        IRulesApplying rules = Rulesets.Find(options.RulesetId);
        ImmutableList<string> botIds = options.Bots ?? [.. Enumerable.Repeat(RandomBot.BotId, SeatCount(rules))];
        return new SelfPlay(rules, botIds, [.. botIds.Select(Bots.Find)], new RandomDice(Random.Shared));
    }

    private static int SeatCount(IRulesApplying rules) => rules.CreateInitialState().Seats.Count;

    /// <summary>The file to write, or standard output, left open for its owner to close.</summary>
    private static TextWriter Output(string? path, TextWriter standardOutput) =>
        path is null ? new NonClosingWriter(standardOutput) : File.CreateText(path);
}
