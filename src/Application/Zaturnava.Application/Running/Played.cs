using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Application.Running;

/// <summary>Where a game has got to, and every action that got it there.</summary>
public sealed record Played(GameState State, ImmutableList<ActionDescriptor> Actions);
