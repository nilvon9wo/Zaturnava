using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.BotRunner;

/// <summary>
/// One game, as training data: which ruleset, which bot played each seat, every action in order,
/// the final scores by seat, and whether the game ended (rather than reaching the step limit).
/// Replaying the actions through the ruleset rebuilds every position.
/// </summary>
internal sealed record GameLog(
    string RulesetId,
    ImmutableList<string> Players,
    bool IsFinished,
    ImmutableSortedDictionary<int, int> Scores,
    ImmutableList<ActionDescriptor> Actions
);
