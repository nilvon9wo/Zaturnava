using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>What a bot may choose: anything legal but handing its turn to an ally; it plays its own.</summary>
internal static class Playable
{
    public static ImmutableList<GameAction> Of(DecisionRequest request) =>
        [.. request.LegalActions.Where(action => action is not HandOverAction)];
}
