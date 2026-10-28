using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Events;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>What applying one action produced: the next state and what happened on the way.</summary>
public sealed record Transition(GameState State, ImmutableList<GameEvent> Events);
