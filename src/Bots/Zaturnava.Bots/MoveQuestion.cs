using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Bots;

/// <summary>
/// What a bot asks its advisor (<see cref="IMoveAdvising"/>): the game as it stands, in words, and the moves it
/// is choosing among, in words too, the one it likes best first.
/// </summary>
public sealed record MoveQuestion(string Situation, ImmutableList<string> Choices);
