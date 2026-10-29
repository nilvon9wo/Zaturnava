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
/// step limit has no outcome.
/// </summary>
internal sealed class SelfPlay(
    IRulesApplying rules,
    ImmutableList<string> botIds,
    ImmutableList<IDeciding> players,
    IDiceRolling dice
)
{
    private static readonly JsonSerializerOptions Lines = JsonSerializerOptions.Web;

    private readonly IRulesApplying _rules = rules;
    private readonly ImmutableList<string> _botIds = botIds;
    private readonly ImmutableList<IDeciding> _players = players;
    private readonly IDiceRolling _dice = dice;

    public Task Run(int games, int maximumSteps, [Required] TextWriter output, CancellationToken cancellationToken)
    {
        TextWriter lines = TextWriter.Synchronized(output);
        return Parallel.ForEachAsync(
            Enumerable.Range(0, games),
            cancellationToken,
            async (_, token) => await lines.WriteLineAsync(await this.PlayOne(maximumSteps, token))
        );
    }

    private async Task<string> PlayOne(int maximumSteps, CancellationToken cancellationToken)
    {
        Played played = await DecisionLoop.PlayAll(
            this._rules,
            this._players,
            this._dice,
            maximumSteps,
            cancellationToken
        );
        GameHistory history = GameHistory.Of(this._rules, played.State, this._botIds, played.Actions);
        return JsonSerializer.Serialize(history, Lines);
    }
}
