using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Plays games of bots against one another, in parallel (a ruleset is a pure function, and bots
/// share nothing but what they are given), and writes each as one JSON line as soon as it ends: its
/// <see cref="GameHistory"/>, the same shape a server gives a hosted game's in. A game that reached the
/// step limit has no outcome. With a <paramref name="seed"/>, each game's dice are its own, from the seed and the
/// game's number, and the lines are written in the games' order once all are played: so that the same bots, seated
/// the other way round, may play each game again with the same dice, and luck tell less between them (the user's
/// thought of 2026-11-27, of testing bots more efficiently).
/// </summary>
internal sealed class SelfPlay(
    IRulesApplying rules,
    ImmutableList<string> botIds,
    ImmutableList<IDeciding> players,
    IDiceRolling dice,
    int? seed = null
)
{
    private static readonly JsonSerializerOptions Lines = JsonSerializerOptions.Web;

    private readonly IRulesApplying _rules = rules;
    private readonly ImmutableList<string> _botIds = botIds;
    private readonly ImmutableList<IDeciding> _players = players;
    private readonly IDiceRolling _dice = dice;
    private readonly int? _seed = seed;

    public Task Run(int games, int maximumSteps, [Required] TextWriter output, CancellationToken cancellationToken) =>
        this._seed is { } seed
            ? this.RunInOrder(games, maximumSteps, seed, output, cancellationToken)
            : this.RunAsTheyEnd(games, maximumSteps, output, cancellationToken);

    private Task RunAsTheyEnd(int games, int maximumSteps, TextWriter output, CancellationToken cancellationToken)
    {
        TextWriter lines = TextWriter.Synchronized(output);
        return Parallel.ForEachAsync(
            Enumerable.Range(0, games),
            cancellationToken,
            async (_, token) =>
            {
                // each game's line out as it ends, so that whoever counts them sees the games played so far
                await lines.WriteLineAsync(await this.PlayOne(this._dice, maximumSteps, token));
                await lines.FlushAsync(token);
            }
        );
    }

    private async Task RunInOrder(
        int games,
        int maximumSteps,
        int seed,
        TextWriter output,
        CancellationToken cancellationToken
    )
    {
        string[] lines = new string[games];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, games),
            cancellationToken,
            async (game, token) =>
                lines[game] = await this.PlayOne(new RandomDice(new Random(seed + game)), maximumSteps, token)
        );
        foreach (string line in lines)
        {
            await output.WriteLineAsync(line);
        }
    }

    private async Task<string> PlayOne(IDiceRolling dice, int maximumSteps, CancellationToken cancellationToken)
    {
        Played played = await DecisionLoop.PlayAll(
            this._rules,
            this._players,
            dice,
            maximumSteps,
            cancellationToken
        );
        GameHistory history = GameHistory.Of(this._rules, played.State, this._botIds, played.Actions);
        return JsonSerializer.Serialize(history, Lines);
    }
}
