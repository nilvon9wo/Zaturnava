using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// Where games may start other than at the beginning: in games played before, at the start of the first turn with no
/// more than so many pieces on the board, so that bots compared on how they end a game spend every game where it
/// shows, not an hour of play alike before it (the user's thought of 2026-11-27, of testing bots more efficiently:
/// "not having every test game start from scratch").
/// </summary>
internal static class Starts
{
    /// <summary>
    /// The games in <paramref name="text"/> - one game's history to a line, or all of them in one JSON array, as the
    /// server gives them - each where it first comes down to <paramref name="pieces"/> pieces or fewer as a turn
    /// begins; but games of other rulesets, games that never do, games that end first, and games that no longer replay
    /// by the rules as they are now. A scenario among them, or alone (<see cref="Scenario"/>), is a game set up by
    /// hand, which starts where it is set up, if it has so few pieces (the user, 2026-11-27: endgames "not necessarily
    /// endgames they reached on their own").
    /// </summary>
    public static ImmutableList<Played> From([Required] IRulesApplying rules, [Required] string text, int pieces) =>
        [
            .. HistoriesIn(text)
                .Where(history => history.RulesetId == rules.Id)
                .Select(history => Reached(rules, history, pieces))
                .OfType<Played>(),
        ];

    private static IEnumerable<GameHistory> HistoriesIn(string text) =>
        EntriesIn(text).Select(entry => entry.TryGetProperty("actions", out _)
            ? entry.Deserialize<GameHistory>(JsonSerializerOptions.Web)!
            : Begun(entry.Deserialize<Scenario>(JsonSerializerOptions.Web)!)
        );

    /// <summary>The text's one JSON array's entries, or its one entry, or else its lines, one entry each.</summary>
    private static ImmutableList<JsonElement> EntriesIn(string text)
    {
        try
        {
            JsonElement whole = JsonElement.Parse(text);
            return whole.ValueKind == JsonValueKind.Array ? [.. whole.EnumerateArray()] : [whole];
        }
        catch (JsonException)
        {
            return
            [
                .. text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(line => JsonElement.Parse(line)),
            ];
        }
    }

    /// <summary>A game begun from the scenario, and no further.</summary>
    private static GameHistory Begun(Scenario scenario) =>
        new(scenario.RulesetId, [], [ScenarioStart.Describe(scenario)], null);

    /// <summary>Where the game first comes down to so few pieces as a turn begins, and how; else none.</summary>
    private static Played? Reached(IRulesApplying rules, GameHistory history, int pieces)
    {
        try
        {
            GameState state = ScenarioStart.StateOf(history.Actions, rules);
            ImmutableList<ActionDescriptor> sofar = [.. history.Actions.Where(ScenarioStart.IsStart)];
            // a game begins undecided, so a game set up with so few pieces starts where it is set up
            if (state.Position.Occupants.Count <= pieces)
            {
                return new Played(state, sofar);
            }

            foreach (ActionDescriptor action in history.Actions.Where(action => !ScenarioStart.IsStart(action)))
            {
                GameState next = rules.Apply(state, rules.Parse(action)).State;
                sofar = sofar.Add(action);
                bool begins = next.TurnNumber != state.TurnNumber;
                state = next;
                if (state.Outcome is null && begins && state.Position.Occupants.Count <= pieces)
                {
                    return new Played(state, sofar);
                }
            }

            return null;
        }
        catch (IllegalActionException)
        {
            return null;
        }
    }
}
